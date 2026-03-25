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

        // Cannon state (row index 0..GridHeight)
        private int cannon1Y;
        private int cannon2Y;

        // Shooting state
        private enum ActionMode { PlacePoint, Shoot }
        private ActionMode currentMode = ActionMode.PlacePoint;
        private int shotPower = 0; // 1-9
        private Label lblStatus;

        // Ball animation — moves purely horizontally
        private System.Windows.Forms.Timer ballTimer;
        private float ballX, ballY;
        private float ballDx;          // horizontal speed (+right, -left)
        private float ballDistTraveled;
        private float ballMaxDist;
        private bool ballFlying = false;
        private int ballRow;           // grid row the ball travels on
        private int ballTargetXGrid;   // the final Grid X intersection the ball targets

        public GameForm(Game game, DatabaseService dbService)
        {
            this.game = game;
            this.dbService = dbService;
            this.logic = new GameLogic(game.GridWidth, game.GridHeight);
            
            if (game.Id > 0)
                this.moves = dbService.GetMoves(game.Id);
            else
                this.moves = new List<Move>();
            
            winLines = this.logic.ReplayAndGetWinLines(moves);

            cannon1Y = game.GridHeight / 2;
            cannon2Y = game.GridHeight / 2;

            this.KeyPreview = true;
            this.KeyDown += GameForm_KeyDown;

            int panelWidth = cannonMargin + Math.Max(0, game.GridWidth - 1) * cellSize + cannonMargin;
            int panelHeight = Math.Max(0, game.GridHeight - 1) * cellSize;

            UpdateFormTitle();
            this.Size = new Size(Math.Max(560, panelWidth + 60), panelHeight + 140);
            this.StartPosition = FormStartPosition.CenterScreen;

            gridPanel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(panelWidth + 1, panelHeight + 1),
                BackColor = Color.White
            };
            gridPanel.Paint += GridPanel_Paint;
            // In shoot mode, clicking the cannon areas repositions them
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
                    shotPower = 0;
                } else {
                    currentMode = ActionMode.PlacePoint;
                    btnToggleMode.Text = "Switch to Shoot";
                    shotPower = 0;
                }
                UpdateStatus();
                gridPanel.Invalidate();
            };

            // Fire button: enabled when in shoot mode and power is selected
            Button btnFire = new Button { Text = "Fire!", Location = new Point(260, bottomY), Size = new Size(80, 30), Enabled = false };
            btnFire.Click += (s, e) => {
                if (ballFlying) return;
                if (currentMode == ActionMode.Shoot && shotPower > 0)
                    FireBall();
            };
            this.Tag = btnFire;

            lblStatus = new Label { Location = new Point(350, bottomY + 5), AutoSize = true, Font = new Font("Consolas", 9) };
            UpdateStatus();

            this.Controls.Add(gridPanel);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnToggleMode);
            this.Controls.Add(btnFire);
            this.Controls.Add(lblStatus);

            ballTimer = new System.Windows.Forms.Timer { Interval = 30 };
            ballTimer.Tick += BallTimer_Tick;
        }

        private int IntersectionPxX(int gx) => cannonMargin + gx * cellSize;
        private int IntersectionPxY(int gy) => gy * cellSize;

        private void UpdateStatus()
        {
            int cannonRow = game.CurrentTurn == 1 ? cannon1Y : cannon2Y;
            if (currentMode == ActionMode.PlacePoint)
                lblStatus.Text = $"Mode: Place Point | P{game.CurrentTurn}'s turn";
            else
            {
                string pwr = shotPower > 0 ? shotPower.ToString() : "none (Ctrl+1-9)";
                lblStatus.Text = $"Mode: Shoot | Canon row: {cannonRow} | Power: {pwr}";
            }

            if (this.Tag is Button fireBtn)
                fireBtn.Enabled = (currentMode == ActionMode.Shoot && shotPower > 0 && !ballFlying);
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

            // Arrow Up/Down move the active player's cannon
            if (e.KeyCode == Keys.Up)
            {
                if (game.CurrentTurn == 1) { if (cannon1Y > 0) cannon1Y--; }
                else                       { if (cannon2Y > 0) cannon2Y--; }
                gridPanel.Invalidate();
                UpdateStatus();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (game.CurrentTurn == 1) { if (cannon1Y < game.GridHeight - 1) cannon1Y++; }
                else                       { if (cannon2Y < game.GridHeight - 1) cannon2Y++; }
                gridPanel.Invalidate();
                UpdateStatus();
                e.Handled = true;
            }

            // Ctrl + 1-9 selects power
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
            ballRow = cannonRow;
            ballY = IntersectionPxY(cannonRow);

            int targetCells = (int)Math.Round((double)shotPower * game.GridWidth / 9.0, MidpointRounding.AwayFromZero);
            ballTargetXGrid = game.CurrentTurn == 1 ? targetCells - 1 : game.GridWidth - targetCells;
            ballTargetXGrid = Math.Max(0, Math.Min(ballTargetXGrid, game.GridWidth - 1));

            float startPxX;
            if (game.CurrentTurn == 1)
            {
                // P1: fires from left → right
                startPxX = cannonMargin - 10;
                ballDx = 6f;
            }
            else
            {
                // P2: fires from right → left
                startPxX = cannonMargin + Math.Max(0, game.GridWidth - 1) * cellSize + 10;
                ballDx = -6f;
            }

            ballX = startPxX;
            float targetPxX = IntersectionPxX(ballTargetXGrid);
            ballMaxDist = Math.Abs(targetPxX - startPxX);
            ballDistTraveled = 0;

            ballFlying = true;
            ballTimer.Start();
            UpdateStatus();
        }

        private void BallTimer_Tick(object sender, EventArgs e)
        {
            ballX += ballDx;
            ballDistTraveled += Math.Abs(ballDx);

            // Reached destination distance
            if (ballDistTraveled >= ballMaxDist)
            {
                bool hit = false;
                // Only effectively "hit" if the target grid point is within bounds
                if (ballTargetXGrid >= 0 && ballTargetXGrid < game.GridWidth)
                {
                    if (logic.RemovePoint(ballTargetXGrid, ballRow, game.CurrentTurn))
                    {
                        moves.RemoveAll(m => m.X == ballTargetXGrid && m.Y == ballRow);
                        for (int i = 0; i < moves.Count; i++) moves[i].MoveOrder = i + 1;
                        hit = true;
                    }
                }
                EndShot(hit);
                return;
            }

            // Out of grid bounds
            if (ballX < cannonMargin - 20 || ballX > cannonMargin + Math.Max(0, game.GridWidth - 1) * cellSize + 20)
            {
                EndShot(false);
                return;
            }

            gridPanel.Invalidate();
        }

        private void EndShot(bool hit)
        {
            ballTimer.Stop();
            ballFlying = false;
            shotPower = 0;

            // If hit an opponent point → shooter keeps turn; otherwise pass
            if (!hit)
                game.CurrentTurn = game.CurrentTurn == 1 ? 2 : 1;
            
            UpdateFormTitle();
            UpdateStatus();
            gridPanel.Invalidate();
        }

        private void GridPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (ballFlying) return;

            int gx = (int)Math.Round((double)(e.X - cannonMargin) / cellSize);
            int gy = (int)Math.Round((double)e.Y / cellSize);
            gy = Math.Max(0, Math.Min(gy, game.GridHeight));

            if (currentMode == ActionMode.PlacePoint)
            {
                if (gx >= 0 && logic.IsValidMove(gx, gy))
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
                        // Keep turn on score
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
                // Click on cannon area to reposition it
                int snappedY = gy;
                if (game.CurrentTurn == 1 && e.X < cannonMargin)
                {
                    cannon1Y = snappedY;
                    UpdateStatus();
                    gridPanel.Invalidate();
                }
                else if (game.CurrentTurn == 2 && e.X > cannonMargin + Math.Max(0, game.GridWidth - 1) * cellSize)
                {
                    cannon2Y = snappedY;
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

            // Grid lines
            Pen gridPen = new Pen(Color.LightGray, 1);
            for (int i = 0; i < game.GridWidth; i++)
                g.DrawLine(gridPen, IntersectionPxX(i), IntersectionPxY(0), IntersectionPxX(i), IntersectionPxY(game.GridHeight - 1));
            for (int j = 0; j < game.GridHeight; j++)
                g.DrawLine(gridPen, IntersectionPxX(0), IntersectionPxY(j), IntersectionPxX(game.GridWidth - 1), IntersectionPxY(j));

            Color c1 = ColorTranslator.FromHtml(game.Player1Color);
            Color c2 = ColorTranslator.FromHtml(game.Player2Color);

            // Highlight cannon row when in shoot mode
            if (currentMode == ActionMode.Shoot && !ballFlying)
            {
                int activeRow = game.CurrentTurn == 1 ? cannon1Y : cannon2Y;
                int ry = IntersectionPxY(activeRow);
                using var rowBrush = new SolidBrush(Color.FromArgb(30, Color.Orange));
                g.FillRectangle(rowBrush, IntersectionPxX(0), ry - pointRadius, game.GridWidth * cellSize, pointRadius * 2);

                if (shotPower > 0)
                {
                    int targetCells = (int)Math.Round((double)shotPower * game.GridWidth / 9.0, MidpointRounding.AwayFromZero);
                    int targetXGrid = game.CurrentTurn == 1 ? targetCells - 1 : game.GridWidth - targetCells;
                    targetXGrid = Math.Max(0, Math.Min(targetXGrid, game.GridWidth - 1));

                    using var powerPen = new Pen(Color.Red, 3) { DashStyle = DashStyle.Dash };
                    int startX = game.CurrentTurn == 1 ? IntersectionPxX(0) - 10 : IntersectionPxX(game.GridWidth - 1) + 10;
                    int endX = IntersectionPxX(targetXGrid);

                    g.DrawLine(powerPen, startX, ry, endX, ry);

                    // Crosshair exactly at the target intersection
                    using var strongRed = new Pen(Color.Red, 2);
                    g.DrawLine(strongRed, endX - 6, ry - 6, endX + 6, ry + 6);
                    g.DrawLine(strongRed, endX - 6, ry + 6, endX + 6, ry - 6);
                }
            }

            // Points
            foreach (var m in moves)
            {
                Color color = m.PlayerNumber == 1 ? c1 : c2;
                using var brush = new SolidBrush(color);
                int px = IntersectionPxX(m.X);
                int py = IntersectionPxY(m.Y);
                g.FillEllipse(brush, px - pointRadius, py - pointRadius, pointRadius * 2, pointRadius * 2);
            }

            // Win lines
            foreach (var win in winLines)
            {
                Color color = win.Player == 1 ? c1 : c2;
                using var pen = new Pen(color, 4);
                g.DrawLine(pen, IntersectionPxX(win.StartWinPoint.X), IntersectionPxY(win.StartWinPoint.Y),
                                IntersectionPxX(win.EndWinPoint.X), IntersectionPxY(win.EndWinPoint.Y));
            }

            // Ball
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
                cx = cannonMargin + Math.Max(0, game.GridWidth - 1) * cellSize + 5;
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
