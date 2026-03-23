using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using PointGame.Models;
using PointGame.Services;

namespace PointGame.Forms
{
    public class GameForm : Form
    {
        private Game game;
        private DatabaseService dbService;
        private GameLogic logic;
        private List<Move> moves;
        private Panel gridPanel;
        
        private int cellSize = 30;
        private int pointRadius = 8;
        private int cannonMargin = 40;
        
        private List<WinResult> winLines = new List<WinResult>();

        // Cannon state
        private int cannon1Y;
        private int cannon2Y;

        // Shooting state
        private enum ActionMode { PlacePoint, Shoot }
        private ActionMode currentMode = ActionMode.PlacePoint;
        private Point? targetCell = null;
        private int shotPower = 0;
        private Label lblStatus;

        // Ball animation
        private System.Windows.Forms.Timer ballTimer;
        private float ballX, ballY;
        private float ballDx, ballDy;
        private float ballDistTraveled;
        private float ballMaxDist;
        private bool ballFlying = false;

        public GameForm(Game game, DatabaseService dbService)
        {
            this.game = game;
            this.dbService = dbService;
            this.logic = new GameLogic(game.GridWidth, game.GridHeight);
            
            if (game.Id > 0) {
                this.moves = dbService.GetMoves(game.Id);
            } else {
                this.moves = new List<Move>();
            }
            
            winLines = this.logic.ReplayAndGetWinLines(moves);

            cannon1Y = game.GridHeight / 2;
            cannon2Y = game.GridHeight / 2;

            this.KeyPreview = true;
            this.KeyDown += GameForm_KeyDown;

            // Panel dimensions: grid has W*cellSize width and H*cellSize height
            // Points sit on intersections (0..W, 0..H)
            int panelWidth = cannonMargin + game.GridWidth * cellSize + cannonMargin;
            int panelHeight = game.GridHeight * cellSize;

            UpdateFormTitle();
            this.Size = new Size(Math.Max(500, panelWidth + 60), panelHeight + 140);
            this.StartPosition = FormStartPosition.CenterScreen;

            gridPanel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(panelWidth + 1, panelHeight + 1),
                BackColor = Color.White
            };
            gridPanel.Paint += GridPanel_Paint;
            gridPanel.MouseClick += GridPanel_MouseClick;

            int bottomY = panelHeight + 30;

            Button btnSave = new Button { Text = "Save Game", Location = new Point(20, bottomY), Size = new Size(100, 30) };
            btnSave.Click += BtnSave_Click;

            Button btnToggleMode = new Button { Text = "Switch to Shoot", Location = new Point(130, bottomY), Size = new Size(120, 30) };
            btnToggleMode.Click += (s, e) => {
                if (ballFlying) return;
                if (currentMode == ActionMode.PlacePoint) {
                    currentMode = ActionMode.Shoot;
                    btnToggleMode.Text = "Switch to Place";
                    targetCell = null; shotPower = 0;
                } else {
                    currentMode = ActionMode.PlacePoint;
                    btnToggleMode.Text = "Switch to Shoot";
                    targetCell = null; shotPower = 0;
                }
                UpdateStatus();
                gridPanel.Invalidate();
            };

            Button btnFire = new Button { Text = "Fire!", Location = new Point(260, bottomY), Size = new Size(80, 30), Enabled = false };
            btnFire.Click += (s, e) => {
                if (ballFlying) return;
                if (currentMode == ActionMode.Shoot && targetCell.HasValue && shotPower > 0)
                    FireBall();
            };

            lblStatus = new Label { Location = new Point(350, bottomY + 5), AutoSize = true, Font = new Font("Consolas", 9) };
            UpdateStatus();

            this.Controls.Add(gridPanel);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnToggleMode);
            this.Controls.Add(btnFire);
            this.Controls.Add(lblStatus);

            this.Tag = btnFire;

            ballTimer = new System.Windows.Forms.Timer { Interval = 30 };
            ballTimer.Tick += BallTimer_Tick;
        }

        // Convert intersection grid coordinate to pixel X on the panel
        private int IntersectionPxX(int gx) => cannonMargin + gx * cellSize;
        // Convert intersection grid coordinate to pixel Y on the panel
        private int IntersectionPxY(int gy) => gy * cellSize;

        private void UpdateStatus()
        {
            if (currentMode == ActionMode.PlacePoint)
                lblStatus.Text = $"Mode: Place Point | P{game.CurrentTurn}'s turn";
            else
            {
                string tgt = targetCell.HasValue ? $"({targetCell.Value.X},{targetCell.Value.Y})" : "none";
                string pwr = shotPower > 0 ? shotPower.ToString() : "none";
                lblStatus.Text = $"Mode: Shoot | Target: {tgt} | Power: {pwr}";
            }

            if (this.Tag is Button fireBtn)
            {
                fireBtn.Enabled = (currentMode == ActionMode.Shoot && targetCell.HasValue && shotPower > 0 && !ballFlying);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (game.Id == 0)
                    game.Id = dbService.CreateGame(game);
                dbService.SaveMovesBulk(game.Id, moves);
                dbService.UpdateGameState(game.Id, game.CurrentTurn, game.Player1Score, game.Player2Score);
                UpdateFormTitle();
                MessageBox.Show("Game saved successfully!", "Save");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving game: " + ex.Message);
            }
        }

        private void UpdateFormTitle()
        {
            this.Text = $"Game {(game.Id == 0 ? "(Unsaved)" : "#" + game.Id)} | Turn: P{game.CurrentTurn} | P1: {game.Player1Score} pts | P2: {game.Player2Score} pts";
        }

        private void GameForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (ballFlying) return;

            if (e.KeyCode == Keys.Up)
            {
                if (game.CurrentTurn == 1) { if (cannon1Y > 0) cannon1Y--; }
                else { if (cannon2Y > 0) cannon2Y--; }
                gridPanel.Invalidate();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (game.CurrentTurn == 1) { if (cannon1Y < game.GridHeight) cannon1Y++; }
                else { if (cannon2Y < game.GridHeight) cannon2Y++; }
                gridPanel.Invalidate();
                e.Handled = true;
            }

            if (currentMode == ActionMode.Shoot && e.Control)
            {
                int num = -1;
                if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9) num = e.KeyCode - Keys.D0;
                if (e.KeyCode >= Keys.NumPad1 && e.KeyCode <= Keys.NumPad9) num = e.KeyCode - Keys.NumPad0;
                if (num >= 1 && num <= 9)
                {
                    shotPower = num;
                    UpdateStatus();
                    e.Handled = true;
                }
            }
        }

        private void FireBall()
        {
            int cannonRow = game.CurrentTurn == 1 ? cannon1Y : cannon2Y;
            
            if (game.CurrentTurn == 1)
                ballX = cannonMargin - 10;
            else
                ballX = cannonMargin + game.GridWidth * cellSize + 10;
            
            ballY = IntersectionPxY(cannonRow);

            float targetPxX = IntersectionPxX(targetCell.Value.X);
            float targetPxY = IntersectionPxY(targetCell.Value.Y);

            float dx = targetPxX - ballX;
            float dy = targetPxY - ballY;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            if (dist == 0) return;

            // Power as rule of three: power/9 * max(gridW, gridH) cells
            int maxDim = Math.Max(game.GridWidth, game.GridHeight);
            ballMaxDist = ((float)shotPower / 9f) * maxDim * cellSize;
            ballDistTraveled = 0;

            float speed = 5;
            ballDx = dx / dist * speed;
            ballDy = dy / dist * speed;

            ballFlying = true;
            ballTimer.Start();
        }

        private void BallTimer_Tick(object sender, EventArgs e)
        {
            ballX += ballDx;
            ballY += ballDy;
            ballDistTraveled += (float)Math.Sqrt(ballDx * ballDx + ballDy * ballDy);

            // Ball ran out of power
            if (ballDistTraveled >= ballMaxDist)
            {
                EndShot(false);
                return;
            }

            // Check out of bounds
            if (ballX < cannonMargin - 20 || ballX > cannonMargin + game.GridWidth * cellSize + 20 ||
                ballY < -20 || ballY > game.GridHeight * cellSize + 20)
            {
                EndShot(false);
                return;
            }

            // Check proximity to every intersection point
            for (int gx = 0; gx <= game.GridWidth; gx++)
            {
                for (int gy = 0; gy <= game.GridHeight; gy++)
                {
                    float ipx = IntersectionPxX(gx);
                    float ipy = IntersectionPxY(gy);
                    float d = (float)Math.Sqrt((ballX - ipx) * (ballX - ipx) + (ballY - ipy) * (ballY - ipy));
                    if (d < pointRadius)
                    {
                        if (logic.RemovePoint(gx, gy, game.CurrentTurn))
                        {
                            moves.RemoveAll(m => m.X == gx && m.Y == gy);
                            for (int i = 0; i < moves.Count; i++) moves[i].MoveOrder = i + 1;
                            EndShot(true);
                            return;
                        }
                    }
                }
            }

            gridPanel.Invalidate();
        }

        private void EndShot(bool hit)
        {
            ballTimer.Stop();
            ballFlying = false;
            targetCell = null;
            shotPower = 0;

            game.CurrentTurn = game.CurrentTurn == 1 ? 2 : 1;
            
            UpdateFormTitle();
            UpdateStatus();
            gridPanel.Invalidate();
        }

        private void GridPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (ballFlying) return;

            // Snap to nearest intersection
            int gx = (int)Math.Round((double)(e.X - cannonMargin) / cellSize);
            int gy = (int)Math.Round((double)e.Y / cellSize);

            if (currentMode == ActionMode.PlacePoint)
            {
                if (logic.IsValidMove(gx, gy))
                {
                    var move = new Move
                    {
                        GameId = game.Id,
                        X = gx,
                        Y = gy,
                        PlayerNumber = game.CurrentTurn,
                        MoveOrder = moves.Count + 1
                    };

                    moves.Add(move);
                    var wins = logic.PlaceMove(gx, gy, game.CurrentTurn);
                    
                    if (wins.Count > 0)
                    {
                        winLines.AddRange(wins);
                        if (game.CurrentTurn == 1) game.Player1Score += wins.Count;
                        else game.Player2Score += wins.Count;
                    }
                    else
                    {
                        game.CurrentTurn = game.CurrentTurn == 1 ? 2 : 1;
                    }
                    
                    UpdateFormTitle();
                    UpdateStatus();
                    gridPanel.Invalidate();
                }
            }
            else if (currentMode == ActionMode.Shoot)
            {
                // Click on cannon area to position cannon
                bool clickedCannonArea = false;
                int snappedY = (int)Math.Round((double)e.Y / cellSize);
                snappedY = Math.Max(0, Math.Min(snappedY, game.GridHeight));

                if (game.CurrentTurn == 1 && e.X < cannonMargin)
                {
                    cannon1Y = snappedY;
                    clickedCannonArea = true;
                }
                else if (game.CurrentTurn == 2 && e.X > cannonMargin + game.GridWidth * cellSize)
                {
                    cannon2Y = snappedY;
                    clickedCannonArea = true;
                }

                if (clickedCannonArea)
                {
                    UpdateStatus();
                    gridPanel.Invalidate();
                }
                else if (gx >= 0 && gx <= game.GridWidth && gy >= 0 && gy <= game.GridHeight)
                {
                    targetCell = new Point(gx, gy);
                    UpdateStatus();
                    gridPanel.Invalidate();
                }
            }
        }

        private void GridPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            DrawCannon(g, 1, cannon1Y, ColorTranslator.FromHtml(game.Player1Color));
            DrawCannon(g, 2, cannon2Y, ColorTranslator.FromHtml(game.Player2Color));

            // Draw grid lines
            Pen gridPen = new Pen(Color.LightGray, 1);
            for (int i = 0; i <= game.GridWidth; i++)
                g.DrawLine(gridPen, IntersectionPxX(i), IntersectionPxY(0), IntersectionPxX(i), IntersectionPxY(game.GridHeight));
            for (int j = 0; j <= game.GridHeight; j++)
                g.DrawLine(gridPen, IntersectionPxX(0), IntersectionPxY(j), IntersectionPxX(game.GridWidth), IntersectionPxY(j));

            Color c1 = ColorTranslator.FromHtml(game.Player1Color);
            Color c2 = ColorTranslator.FromHtml(game.Player2Color);

            // Draw points on intersections
            foreach (var m in moves)
            {
                Color color = m.PlayerNumber == 1 ? c1 : c2;
                using var brush = new SolidBrush(color);
                int px = IntersectionPxX(m.X);
                int py = IntersectionPxY(m.Y);
                g.FillEllipse(brush, px - pointRadius, py - pointRadius, pointRadius * 2, pointRadius * 2);
            }

            // Draw win lines
            foreach (var win in winLines)
            {
                Color color = win.Player == 1 ? c1 : c2;
                using var pen = new Pen(color, 4);
                g.DrawLine(pen, IntersectionPxX(win.StartWinPoint.X), IntersectionPxY(win.StartWinPoint.Y),
                                IntersectionPxX(win.EndWinPoint.X), IntersectionPxY(win.EndWinPoint.Y));
            }

            // Draw target marker
            if (currentMode == ActionMode.Shoot && targetCell.HasValue)
            {
                int tx = IntersectionPxX(targetCell.Value.X);
                int ty = IntersectionPxY(targetCell.Value.Y);
                using var targetPen = new Pen(Color.Red, 2);
                g.DrawLine(targetPen, tx - 8, ty, tx + 8, ty);
                g.DrawLine(targetPen, tx, ty - 8, tx, ty + 8);
                g.DrawEllipse(targetPen, tx - 10, ty - 10, 20, 20);
            }

            // Draw ball
            if (ballFlying)
            {
                using var ballBrush = new SolidBrush(Color.Black);
                g.FillEllipse(ballBrush, ballX - 5, ballY - 5, 10, 10);
            }
        }

        private void DrawCannon(Graphics g, int player, int cannonRow, Color color)
        {
            int cannonW = 30;
            int cannonH = 20;
            int cx, cy;

            cy = IntersectionPxY(cannonRow) - cannonH / 2;

            if (player == 1)
            {
                cx = 5;
                using var brush = new SolidBrush(color);
                g.FillRectangle(brush, cx, cy, cannonW, cannonH);
                g.FillRectangle(brush, cx + cannonW, cy + cannonH / 2 - 3, 10, 6);
            }
            else
            {
                cx = cannonMargin + game.GridWidth * cellSize + 5;
                using var brush = new SolidBrush(color);
                g.FillRectangle(brush, cx, cy, cannonW, cannonH);
                g.FillRectangle(brush, cx - 10, cy + cannonH / 2 - 3, 10, 6);
            }

            if (game.CurrentTurn == player)
            {
                using var highlightPen = new Pen(Color.Gold, 2);
                if (player == 1)
                    g.DrawRectangle(highlightPen, 4, cy - 1, cannonW + 12, cannonH + 2);
                else
                    g.DrawRectangle(highlightPen, cx - 11, cy - 1, cannonW + 12, cannonH + 2);
            }
        }
    }
}
