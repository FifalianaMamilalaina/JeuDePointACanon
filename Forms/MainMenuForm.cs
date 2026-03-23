using System;
using System.Drawing;
using System.Windows.Forms;
using PointGame.Services;

namespace PointGame.Forms
{
    public class MainMenuForm : Form
    {
        private DatabaseService dbService;

        public MainMenuForm()
        {
            dbService = new DatabaseService();

            this.Text = "Point Game";
            this.Size = new Size(300, 200);
            this.StartPosition = FormStartPosition.CenterScreen;

            Button btnNewGame = new Button { Text = "New Game", Location = new Point(100, 40), Size = new Size(80, 30) };
            btnNewGame.Click += (s, e) => {
                var setupForm = new GameSetupForm(dbService);
                this.Hide();
                setupForm.ShowDialog();
                this.Show();
            };

            Button btnLoadGame = new Button { Text = "Load Game", Location = new Point(100, 90), Size = new Size(80, 30) };
            btnLoadGame.Click += (s, e) => {
                var loadForm = new LoadGameForm(dbService);
                this.Hide();
                loadForm.ShowDialog();
                this.Show();
            };

            this.Controls.Add(btnNewGame);
            this.Controls.Add(btnLoadGame);
        }
    }
}
