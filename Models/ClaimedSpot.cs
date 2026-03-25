namespace PointGame.Models
{
    public class ClaimedSpot
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int PlayerNumber { get; set; }
    }
}
