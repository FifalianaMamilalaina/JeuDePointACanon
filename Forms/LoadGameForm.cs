using System;
using System.Windows.Forms;
using PointGame.Models;
using PointGame.Services;

namespace PointGame.Forms
{
    public class LoadGameForm : Form
    {
        private DatabaseService dbService;
        private ListBox listGames;

        public LoadGameForm(DatabaseService db)
        {
            this.dbService = db;
            this.Text = "Load / Delete Game";
            this.Size = new System.Drawing.Size(350, 300);
            this.StartPosition = FormStartPosition.CenterParent;

            listGames = new ListBox { Location = new System.Drawing.Point(20, 20), Size = new System.Drawing.Size(290, 180) };
            
            LoadGamesList();

            Button btnLoad = new Button { Text = "Load", Location = new System.Drawing.Point(60, 210) };
            btnLoad.Click += BtnLoad_Click;
            
            Button btnDelete = new Button { Text = "Delete", Location = new System.Drawing.Point(180, 210) };
            btnDelete.Click += BtnDelete_Click;

            this.Controls.Add(listGames);
            this.Controls.Add(btnLoad);
            this.Controls.Add(btnDelete);
        }

        private void LoadGamesList()
        {
            listGames.Items.Clear();
            try
            {
                var games = dbService.GetAllGames();
                foreach (var g in games)
                {
                    listGames.Items.Add(new GameItem { Game = g, DisplayText = $"Game #{g.Id} | W:{g.GridWidth} H:{g.GridHeight} | P1: {g.Player1Score} P2: {g.Player2Score}" });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("DB Error: " + ex.Message + "\n\nPlease ensure you run the db_setup.sql script and have PostgreSQL running on localhost.");
            }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            if (listGames.SelectedItem is GameItem item)
            {
                var gameForm = new GameForm(item.Game, dbService);
                this.Hide();
                gameForm.ShowDialog();
                this.Close();
            }
            else
            {
                MessageBox.Show("Please select a game to load.");
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (listGames.SelectedItem is GameItem item)
            {
                var result = MessageBox.Show($"Are you sure you want to delete Game #{item.Game.Id}?", "Confirm Delete", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        dbService.DeleteGame(item.Game.Id);
                        LoadGamesList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting game: " + ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a game to delete.");
            }
        }

        private class GameItem
        {
            public Game Game { get; set; }
            public string DisplayText { get; set; }
            public override string ToString() => DisplayText;
        }
    }
}
