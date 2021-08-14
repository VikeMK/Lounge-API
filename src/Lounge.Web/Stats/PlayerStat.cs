namespace Lounge.Web.Stats
{
    public record RankedPlayerStat(int Rank, PlayerStat Stat);

    public class PlayerStat
    {
        public PlayerStat(int id, string name, string normalizedName)
        {
            Id = id;
            Name = name;
            NormalizedName = normalizedName;
        }

        public int Id { get; }
        public string Name { get; }
        public string NormalizedName { get; }
        public string? CountryCode { get; init; }
        public bool IsHidden { get; init; }
        public int? Mmr { get; init; }
        public int? MaxMmr { get; init; }
        public int EventsPlayed { get; init; }
        public int Wins { get; init; }
        public int? LargestGain { get; init; }
        public int? LargestLoss { get; init; }
        public int? LastTenGainLoss { get; init; }
        public int LastTenWins { get; init; }
        public int LastTenLosses { get; init; }
    }
}
