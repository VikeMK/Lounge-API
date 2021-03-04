using Microsoft.EntityFrameworkCore;

namespace Lounge.Web.Models
{
    public class PlayerStat
    {
        public int Rank { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public int? Mmr { get; set; }
        public int? MaxMmr { get; set; }
        public string NormalizedName { get; set; } = default!;
        public int EventsPlayed { get; set; }
        public int Wins { get; set; }
        public int? LargestGain { get; set; }
        public int? LargestLoss { get; set; }
        public int? LastTenGainLoss { get; set; }
        public int LastTenWins { get; set; }
        public int LastTenLosses { get; set; }
    }
}
