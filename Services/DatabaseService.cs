using Npgsql;
using System.Collections.Generic;
using PointGame.Models;

namespace PointGame.Services
{
    public class DatabaseService
    {
        // TODO: Replace with the actual connection string or read from config
        private string connectionString = "Host=localhost;Username=postgres;Password=Fifaliana!;Database=pointgame";

        public DatabaseService()
        {
            EnsureColumnsExist();
        }

        private void EnsureColumnsExist()
        {
            try
            {
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();
                using var cmd = new NpgsqlCommand(@"
                    ALTER TABLE games ADD COLUMN IF NOT EXISTS player1_score INT DEFAULT 0;
                    ALTER TABLE games ADD COLUMN IF NOT EXISTS player2_score INT DEFAULT 0;
                    CREATE TABLE IF NOT EXISTS claimed_spots (
                        id SERIAL PRIMARY KEY,
                        game_id INT REFERENCES games(id) ON DELETE CASCADE,
                        x INT NOT NULL,
                        y INT NOT NULL,
                        player_number INT NOT NULL
                    );", conn);
                cmd.ExecuteNonQuery();
            }
            catch { /* Ignore if it fails, maybe tables don't exist yet */ }
        }

        public int CreateGame(Game game)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("INSERT INTO games (grid_width, grid_height, player1_color, player2_color, current_turn, status, player1_score, player2_score) VALUES (@w, @h, @c1, @c2, @t, 'InProgress', 0, 0) RETURNING id", conn);
            cmd.Parameters.AddWithValue("w", game.GridWidth);
            cmd.Parameters.AddWithValue("h", game.GridHeight);
            cmd.Parameters.AddWithValue("c1", game.Player1Color);
            cmd.Parameters.AddWithValue("c2", game.Player2Color);
            cmd.Parameters.AddWithValue("t", game.CurrentTurn);
            
            return (int)cmd.ExecuteScalar();
        }

        public void SaveMove(Move move)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("INSERT INTO moves (game_id, x, y, player_number, move_order) VALUES (@g, @x, @y, @p, @o)", conn);
            cmd.Parameters.AddWithValue("g", move.GameId);
            cmd.Parameters.AddWithValue("x", move.X);
            cmd.Parameters.AddWithValue("y", move.Y);
            cmd.Parameters.AddWithValue("p", move.PlayerNumber);
            cmd.Parameters.AddWithValue("o", move.MoveOrder);
            cmd.ExecuteNonQuery();
        }

        public void SaveMovesBulk(int gameId, List<Move> moves)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            
            using var cmdDel = new NpgsqlCommand("DELETE FROM moves WHERE game_id = @id", conn);
            cmdDel.Parameters.AddWithValue("id", gameId);
            cmdDel.ExecuteNonQuery();

            foreach (var m in moves)
            {
                using var cmd = new NpgsqlCommand("INSERT INTO moves (game_id, x, y, player_number, move_order) VALUES (@g, @x, @y, @p, @o)", conn);
                cmd.Parameters.AddWithValue("g", gameId);
                cmd.Parameters.AddWithValue("x", m.X);
                cmd.Parameters.AddWithValue("y", m.Y);
                cmd.Parameters.AddWithValue("p", m.PlayerNumber);
                cmd.Parameters.AddWithValue("o", m.MoveOrder);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateGameState(int gameId, int nextTurn, int p1Score, int p2Score)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("UPDATE games SET current_turn = @t, player1_score = @p1, player2_score = @p2 WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("t", nextTurn);
            cmd.Parameters.AddWithValue("p1", p1Score);
            cmd.Parameters.AddWithValue("p2", p2Score);
            cmd.Parameters.AddWithValue("id", gameId);
            cmd.ExecuteNonQuery();
        }

        public void DeleteGame(int gameId)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM games WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", gameId);
            cmd.ExecuteNonQuery();
        }

        public Game GetGame(int gameId)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT * FROM games WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", gameId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Game
                {
                    Id = reader.GetInt32(0),
                    GridWidth = reader.GetInt32(1),
                    GridHeight = reader.GetInt32(2),
                    Player1Color = reader.GetString(3),
                    Player2Color = reader.GetString(4),
                    CurrentTurn = reader.GetInt32(5),
                    Status = reader.GetString(6),
                    Player1Score = reader.FieldCount > 7 ? reader.GetInt32(7) : 0,
                    Player2Score = reader.FieldCount > 8 ? reader.GetInt32(8) : 0
                };
            }
            return null;
        }

        public List<Move> GetMoves(int gameId)
        {
            var moves = new List<Move>();
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT * FROM moves WHERE game_id = @id ORDER BY move_order ASC", conn);
            cmd.Parameters.AddWithValue("id", gameId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                moves.Add(new Move
                {
                    Id = reader.GetInt32(0),
                    GameId = reader.GetInt32(1),
                    X = reader.GetInt32(2),
                    Y = reader.GetInt32(3),
                    PlayerNumber = reader.GetInt32(4),
                    MoveOrder = reader.GetInt32(5)
                });
            }
            return moves;
        }

        public List<Game> GetAllGames()
        {
            var games = new List<Game>();
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, grid_width, grid_height, player1_color, player2_color, current_turn, status, player1_score, player2_score FROM games ORDER BY id DESC", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                games.Add(new Game
                {
                    Id = reader.GetInt32(0),
                    GridWidth = reader.GetInt32(1),
                    GridHeight = reader.GetInt32(2),
                    Player1Color = reader.GetString(3),
                    Player2Color = reader.GetString(4),
                    CurrentTurn = reader.GetInt32(5),
                    Status = reader.GetString(6),
                    Player1Score = reader.GetInt32(7),
                    Player2Score = reader.GetInt32(8)
                });
            }
            return games;
        }

        public void SaveClaimedSpots(int gameId, HashSet<(int X, int Y, int Player)> claimedSpots)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            using var cmdDel = new NpgsqlCommand("DELETE FROM claimed_spots WHERE game_id = @id", conn);
            cmdDel.Parameters.AddWithValue("id", gameId);
            cmdDel.ExecuteNonQuery();

            foreach (var s in claimedSpots)
            {
                using var cmd = new NpgsqlCommand("INSERT INTO claimed_spots (game_id, x, y, player_number) VALUES (@g, @x, @y, @p)", conn);
                cmd.Parameters.AddWithValue("g", gameId);
                cmd.Parameters.AddWithValue("x", s.X);
                cmd.Parameters.AddWithValue("y", s.Y);
                cmd.Parameters.AddWithValue("p", s.Player);
                cmd.ExecuteNonQuery();
            }
        }

        public HashSet<(int X, int Y, int Player)> GetClaimedSpots(int gameId)
        {
            var spots = new HashSet<(int, int, int)>();
            try
            {
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT x, y, player_number FROM claimed_spots WHERE game_id = @id", conn);
                cmd.Parameters.AddWithValue("id", gameId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    spots.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2)));
                }
            }
            catch { /* table may not exist yet */ }
            return spots;
        }
    }
}
