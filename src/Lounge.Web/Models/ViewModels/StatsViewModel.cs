using System.Collections.Generic;

namespace Lounge.Web.Models.ViewModels
{
    public class StatsViewModel
    {
        public int TotalPlayers { get; init; }

        public int TotalMogis { get; init; }

        public double AverageMmr { get; init; }

        public int MedianMmr { get; init; }

        public required List<Division> DivisionData { get; init; }

        public class Division
        {
            public required string Tier { get; init; }

            public int Count { get; init; }

        }

        public required Dictionary<string, Country> CountryData { get; init; }

        public class Country
        {
            public int PlayerTotal { get; set; }

            public double TotalAverageMmr { get; set; }

            public double TopSixMmr { get; set; }

            public required List<Player> TopSixPlayers { get; set; }
        }

        public class Player
        {
            public required string Name { get; init; }

            public int Mmr { get; init; }
        }        public required Activity ActivityData { get; init; }

        public class Activity
        {
            public required Dictionary<string, int> FormatData { get; init; }

            public required Dictionary<string, Dictionary<string, int>> DailyActivity { get; init; }

            public required Dictionary<string, int> DayOfWeekActivity { get; init; }

            public required Dictionary<string, int> TierActivity { get; init; }
        }

        // Season configuration data
        public required IReadOnlyDictionary<string, int> Ranks { get; init; }

        public required IReadOnlyList<string> RecordsTierOrder { get; init; }

        public required IReadOnlyDictionary<string, IReadOnlyList<string>> DivisionsToTier { get; init; }
    }
}