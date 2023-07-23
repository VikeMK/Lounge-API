using System.Collections.Generic;

namespace Lounge.Web.Models.ViewModels
{
    public class StatsViewModel
    {
        public int TotalPlayers { get; init; }

        public int TotalMogis { get; init; }

        public double AverageMmr { get; init; }

        public int MedianMmr { get; init; }

        public List<Division> DivisionData { get; init; }

        public class Division
        {
            public string Tier { get; init; }

            public int Count { get; init; }

        }

        public Dictionary<string, Country> CountryData { get; init; }

        public class Country
        {
            public int PlayerTotal { get; set; }

            public double TotalAverageMmr { get; set; }

            public double TopSixMmr { get; set; }

            public List<Player> TopSixPlayers { get; set; }
        }

        public class Player
        {
            public string Name { get; init; }

            public int Mmr { get; init; }
        }

        public Activity ActivityData { get; init; }

        public class Activity
        {
            public Dictionary<string, int> FormatData { get; init; }

            public Dictionary<string, Dictionary<string, int>> DailyActivity { get; init; }

            public Dictionary<string, int> DayOfWeekActivity { get; init; }

            public Dictionary<string, int> TierActivity { get; init; }
        }
    }
}