using System;
using System.Drawing;
using System.Windows.Forms;
using PointGame.Models;
using PointGame.Services;

namespace PointGame.Forms
{
    public class GameSetupForm : Form
    {
        private NumericUpDown nudWidth;
        private NumericUpDown nudHeight;
        private Button btnColor1;
        private Button btnColor2;
        private Color color1 = Color.Blue;
        private Color color2 = Color.Red;
        private DatabaseService dbService;

        public GameSetupForm(DatabaseService db)
        {
            this.dbService = db;
            this.Text = "Game Setup";
            this.Size = new Size(300, 250);
            this.StartPosition = FormStartPosition.CenterParent;

            Label lblWidth = new Label { Text = "Grid Width:", Location = new Point(20, 20), AutoSize = true };
            nudWidth = new NumericUpDown { Location = new Point(120, 20), Minimum = 5, Maximum = 50, Value = 15 };

            Label lblHeight = new Label { Text = "Grid Height:", Location = new Point(20, 50), AutoSize = true };
            nudHeight = new NumericUpDown { Location = new Point(120, 50), Minimum = 5, Maximum = 50, Value = 15 };

            Label lblP1 = new Label { Text = "Player 1 Color:", Location = new Point(20, 80), AutoSize = true };
            btnColor1 = new Button { Location = new Point(120, 80), BackColor = color1, Width = 50 };
            btnColor1.Click += (s, e) => { color1 = ChooseColor(color1); btnColor1.BackColor = color1; };

            Label lblP2 = new Label { Text = "Player 2 Color:", Location = new Point(20, 110), AutoSize = true };
            btnColor2 = new Button { Location = new Point(120, 110), BackColor = color2, Width = 50 };
            btnColor2.Click += (s, e) => { color2 = ChooseColor(color2); btnColor2.BackColor = color2; };

            Button btnStart = new Button { Text = "Start Game", Location = new Point(100, 160) };
            btnStart.Click += BtnStart_Click;

            this.Controls.Add(lblWidth); this.Controls.Add(nudWidth);
            this.Controls.Add(lblHeight); this.Controls.Add(nudHeight);
            this.Controls.Add(lblP1); this.Controls.Add(btnColor1);
            this.Controls.Add(lblP2); this.Controls.Add(btnColor2);
            this.Controls.Add(btnStart);
        }

        private Color ChooseColor(Color current)
        {
            using (ColorDialog cd = new ColorDialog { Color = current })
            {
                if (cd.ShowDialog() == DialogResult.OK) return cd.Color;
                return current;
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            var game = new Game
            {
                Id = 0,
                GridWidth = (int)nudWidth.Value,
                GridHeight = (int)nudHeight.Value,
                Player1Color = ColorTranslator.ToHtml(color1),
                Player2Color = ColorTranslator.ToHtml(color2),
                CurrentTurn = 1,
                Player1Score = 0,
                Player2Score = 0,
                Status = "InProgress"
            };

            var gameForm = new GameForm(game, dbService);
            this.Hide();
            gameForm.ShowDialog();
            this.Close();
        }
    }
}
