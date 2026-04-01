using System;
using System.Collections.Generic;
using PointGame.Models;
using System.Drawing;

namespace PointGame.Services
{
    public class WinResult
    {
        public bool IsWin { get; set; }
        public Point StartWinPoint { get; set; }
        public Point EndWinPoint { get; set; }
        public int Player { get; set; }
        public List<Point> Points { get; set; } = new List<Point>();
    }

    public class GameLogic
    {
        private int width;  // number of cells horizontally
        private int height; // number of cells vertically
        private int[,] grid;
        public List<WinResult> allLines;
        public HashSet<(int X, int Y, int Player)> claimedSpots = new();

        public GameLogic(int width, int height)
        {
            this.width = width;
            this.height = height;
            grid = new int[width, height];
            allLines = new List<WinResult>();
        }

        public void LoadMoves(List<Move> moves)
        {
            foreach (var move in moves)
            {
                if (move.X >= 0 && move.X < width && move.Y >= 0 && move.Y < height)
                {
                    grid[move.X, move.Y] = move.PlayerNumber;
                }
            }
        }

        public List<WinResult> ReplayAndGetWinLines(List<Move> moves)
        {
            allLines.Clear();
            grid = new int[width, height];

            foreach (var m in moves)
            {
                if (m.X >= 0 && m.X < width && m.Y >= 0 && m.Y < height)
                {
                    grid[m.X, m.Y] = m.PlayerNumber;
                    var wins = CheckWin(m.X, m.Y, m.PlayerNumber);
                    foreach (var w in wins)
                    {
                        allLines.Add(w);
                    }
                }
            }
            return new List<WinResult>(allLines);
        }

        public bool IsValidMove(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return false;
            return grid[x, y] == 0;
        }

        public List<WinResult> PlaceMove(int x, int y, int player)
        {
            if (!IsValidMove(x, y)) return new List<WinResult>();
            grid[x, y] = player;
            
            var wins = CheckWin(x, y, player);
            foreach (var w in wins)
            {
                allLines.Add(w);
            }
            return wins;
        }

        /// <summary>
        /// Check for win lines at a position that is already placed on the grid.
        /// Used after TryReclaim to validate lines without re-placing the point.
        /// </summary>
        public List<WinResult> PlaceMove_CheckOnly(int x, int y, int player)
        {
            var wins = CheckWin(x, y, player);
            foreach (var w in wins)
            {
                allLines.Add(w);
            }
            return wins;
        }

        public bool IsPointInLine(int x, int y)
        {
            Point p = new Point(x, y);
            foreach (var line in allLines)
            {
                if (line.Points.Contains(p)) return true;
            }
            return false;
        }

        public bool RemovePoint(int x, int y, int shooterPlayer)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return false;
            if (grid[x, y] == 0) return false;
            if (grid[x, y] == shooterPlayer) return false; // Cannot destroy own points
            if (IsPointInLine(x, y)) return false; // Cannot destroy line points
            int victim = grid[x, y];
            grid[x, y] = 0;
            // The victim now claims this spot for future reclaim
            claimedSpots.Add((x, y, victim));
            return true;
        }

        /// <summary>
        /// Attempt to reclaim a previously destroyed spot.
        /// Returns true if the spot was reclaimed (point re-placed).
        /// </summary>
        public bool TryReclaim(int x, int y, int shooterPlayer)
        {
            if (!claimedSpots.Contains((x, y, shooterPlayer))) return false;

            int occupant = grid[x, y];
            if (occupant == shooterPlayer) return false; // already has own point there

            if (occupant != 0)
            {
                // Spot occupied by opponent
                if (IsPointInLine(x, y)) return false; // opponent point is in a line, protected
                // Remove opponent point (this creates a claim for them too)
                int opponentPlayer = occupant;
                grid[x, y] = 0;
                claimedSpots.Add((x, y, opponentPlayer));
            }

            // Re-place the shooter's point
            grid[x, y] = shooterPlayer;
            claimedSpots.Remove((x, y, shooterPlayer));
            return true;
        }

        private List<WinResult> CheckWin(int x, int y, int player)
        {
            var wins = new List<WinResult>();

            int[][] directions = new int[][]
            {
                new int[] { 1, 0 },
                new int[] { 0, 1 },
                new int[] { 1, 1 },
                new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                List<Point> currentLine = new List<Point>();
                currentLine.Add(new Point(x, y));

                Point start = new Point(x, y);
                Point end = new Point(x, y);

                for (int i = 1; i < 50; i++)
                {
                    int nx = x + dir[0] * i;
                    int ny = y + dir[1] * i;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height || grid[nx, ny] != player) break;
                    end = new Point(nx, ny);
                    currentLine.Add(end);
                }

                for (int i = 1; i < 50; i++)
                {
                    int nx = x - dir[0] * i;
                    int ny = y - dir[1] * i;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height || grid[nx, ny] != player) break;
                    start = new Point(nx, ny);
                    currentLine.Insert(0, start);
                }

                if (currentLine.Count >= 5)
                {
                    Point pXY = new Point(x, y);
                    int targetIdx = currentLine.IndexOf(pXY);
                    
                    for (int i = 0; i <= currentLine.Count - 5; i++)
                    {
                        if (i <= targetIdx && i + 4 >= targetIdx)
                        {
                            var subLine = currentLine.GetRange(i, 5);
                            if (!DoesLineCrossOpponent(subLine, player) && !SharesMoreThanOnePointWithAnyExistingLine(subLine))
                            {
                                wins.Add(new WinResult 
                                { 
                                    IsWin = true, 
                                    StartWinPoint = subLine[0], 
                                    EndWinPoint = subLine[4], 
                                    Player = player, 
                                    Points = subLine 
                                });
                                break; // only count one line per direction
                            }
                        }
                    }
                }
            }

            return wins;
        }

        private bool SharesMoreThanOnePointWithAnyExistingLine(List<Point> newLine)
        {
            foreach (var existingLine in allLines)
            {
                int sharedCount = 0;
                foreach (var p in newLine)
                {
                    if (existingLine.Points.Contains(p))
                    {
                        sharedCount++;
                    }
                }
                if (sharedCount > 1) return true;
            }
            return false;
        }

        private bool DoesLineCrossOpponent(List<Point> newLine, int player)
        {
            if (newLine.Count < 2) return false;
            
            int dx = Math.Sign(newLine[1].X - newLine[0].X);
            int dy = Math.Sign(newLine[1].Y - newLine[0].Y);

            if (dx != 0 && dy != 0)
            {
                for (int i = 0; i < newLine.Count - 1; i++)
                {
                    Point px = newLine[i];
                    Point pNext = newLine[i + 1];

                    // Opposite crossing diagonal step
                    Point crossA = new Point(pNext.X, px.Y);
                    Point crossB = new Point(px.X, pNext.Y);

                    if (IsSegmentInOpponentLine(crossA, crossB, player))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsSegmentInOpponentLine(Point p1, Point p2, int player)
        {
            foreach (var line in allLines)
            {
                if (line.Player != player)
                {
                    int index1 = line.Points.IndexOf(p1);
                    int index2 = line.Points.IndexOf(p2);
                    if (index1 != -1 && index2 != -1 && Math.Abs(index1 - index2) == 1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Find all empty positions where placing a point would complete a line of exactly 5.
        /// Returns the list of empty positions (suggestions).
        /// </summary>
        public List<Point> FindSuggestionsForFour(int player)
        {
            var suggestions = new HashSet<Point>();
            int[][] directions = new int[][] {
                new int[] { 1, 0 }, new int[] { 0, 1 },
                new int[] { 1, 1 }, new int[] { 1, -1 }
            };

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] != 0) continue; // must be empty

                    // Temporarily place the point
                    grid[x, y] = player;
                    foreach (var dir in directions)
                    {
                        List<Point> line = new List<Point> { new Point(x, y) };
                        for (int i = 1; i < 50; i++)
                        {
                            int nx = x + dir[0] * i, ny = y + dir[1] * i;
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height || grid[nx, ny] != player) break;
                            line.Add(new Point(nx, ny));
                        }
                        for (int i = 1; i < 50; i++)
                        {
                            int nx = x - dir[0] * i, ny = y - dir[1] * i;
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height || grid[nx, ny] != player) break;
                            line.Insert(0, new Point(nx, ny));
                        }
                        if (line.Count >= 5)
                        {
                            suggestions.Add(new Point(x, y));
                            break;
                        }
                    }
                    grid[x, y] = 0; // undo
                }
            }
            return new List<Point>(suggestions);
        }

        /// <summary>
        /// Find all segments of 5 consecutive cells (in any direction) that contain
        /// exactly 3 points of the given player and 2 empty cells.
        /// Returns the list of empty positions from those segments.
        /// </summary>
        public List<Point> FindSuggestionsForThree(int player)
        {
            var suggestions = new HashSet<Point>();
            int[][] directions = new int[][] {
                new int[] { 1, 0 }, new int[] { 0, 1 },
                new int[] { 1, 1 }, new int[] { 1, -1 }
            };

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    foreach (var dir in directions)
                    {
                        // Build a window of 5 cells starting at (x,y)
                        List<Point> segment = new List<Point>();
                        bool valid = true;
                        for (int i = 0; i < 5; i++)
                        {
                            int nx = x + dir[0] * i, ny = y + dir[1] * i;
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height) { valid = false; break; }
                            int cell = grid[nx, ny];
                            if (cell != 0 && cell != player) { valid = false; break; } // opponent blocks this segment
                            segment.Add(new Point(nx, ny));
                        }
                        if (!valid || segment.Count != 5) continue;

                        int playerCount = 0;
                        var empties = new List<Point>();
                        foreach (var p in segment)
                        {
                            if (grid[p.X, p.Y] == player) playerCount++;
                            else empties.Add(p);
                        }

                        if (playerCount == 3 && empties.Count == 2)
                        {
                            foreach (var e in empties)
                                suggestions.Add(e);
                        }
                    }
                }
            }
            return new List<Point>(suggestions);
        }
    }
}
