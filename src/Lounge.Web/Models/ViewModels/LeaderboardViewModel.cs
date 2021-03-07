using Lounge.Web.Models.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Models.ViewModels
{
    public class LeaderboardViewModel
    {
        public List<Player> Players { get; init; }

        public int Page { get; init; }

        public bool HasPrevPage { get; init; }

        public bool HasNextPage { get; init; }

        public string? Filter { get; init; }

        public class Player
        {
            public int Id { get; init; }

            [Display(Name = "Rank")]
            [DisplayFormat(NullDisplayText = "-")]
            public int? OverallRank { get; init; }

            public string Name { get; init; }

            [Display(Name = "MMR")]
            [DisplayFormat(NullDisplayText = "Placement")]
            public int? Mmr { get; init; }

            [Display(Name = "Peak MMR")]
            [DisplayFormat(NullDisplayText = "N/A")]
            public int? MaxMmr { get; init; }

            [Display(Name = "Win Rate")]
            [DisplayFormat(NullDisplayText = "N/A", DataFormatString = "{0:P1}")]
            public decimal? WinRate { get; init; }

            public int WinsLastTen { get; init; }

            public int LossesLastTen { get; init; }

            [Display(Name = "Win - Loss (Last 10)")]
            public string WinLossLastTen => $"{WinsLastTen} - {LossesLastTen}";

            [Display(Name = "Gain/Loss (Last 10)")]
            [DisplayFormat(NullDisplayText = "N/A", DataFormatString = "{0:+#;-#;0}")]
            public int? GainLossLastTen { get; init; }

            [Display(Name = "Events Played")]
            public int EventsPlayed { get; init; }

            [Display(Name = "Largest Gain")]
            [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:+#;-#;0}")]
            public int? LargestGain { get; init; }

            [Display(Name = "Largest Loss")]
            [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:+#;-#;0}")]
            public int? LargestLoss { get; init; }

            public Rank? MmrRank { get; init; }

            public Rank? MaxMmrRank { get; init; }
        }
    }
}
