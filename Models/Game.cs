namespace PointGame.Models
{
    public class Game
    {
        public int Id { get; set; }
        public int GridWidth { get; set; }
        public int GridHeight { get; set; }
        public string Player1Color { get; set; }
        public string Player2Color { get; set; }
        public int CurrentTurn { get; set; } // 1 or 2
        public string Status { get; set; } // InProgress, Player1Won, Player2Won, Draw
        public int Player1Score { get; set; }
        public int Player2Score { get; set; }
    }
}
