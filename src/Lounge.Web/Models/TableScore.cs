namespace Lounge.Web.Models
{
    public class TableScore
    {
        public int Team { get; set; }
        public int Score { get; set; }
        public double Multiplier { get; set; }
        public int? PrevMmr { get; set; }
        public int? NewMmr { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; } = default!;

        public int TableId { get; set; }
        public Table Table { get; set; } = default!;
    }
}
