using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Models.ViewModels
{
    public class LeaderboardViewModel
    {
        public LeaderboardViewModel(List<Player> players)
        {
            Players = players;
        }

        public List<Player> Players { get; set; }

        public class Player
        {
            public int Id { get; init; }

            public int Rank { get; init; }

            public string Name { get; init; }

            [Display(Name = "MMR")]
            [DisplayFormat(NullDisplayText = "Placement")]
            public int? Mmr { get; init; }

            [Display(Name = "Peak MMR")]
            [DisplayFormat(NullDisplayText = "N/A")]
            public int? MaxMmr { get; init; }

            [Display(Name = "Win Rate")]
            [DisplayFormat(NullDisplayText = "N/A")]
            public decimal? WinRate { get; init; }

            public int WinsLastTen { get; init; }

            public int LossesLastTen { get; init; }

            [Display(Name = "Win - Loss (Last 10)")]
            public string WinLossLastTen => $"{WinsLastTen} - {LossesLastTen}";

            [Display(Name = "Gain/Loss (Last 10)")]
            public int GainLossLastTen { get; init; }

            [Display(Name = "Events Played")]
            public int EventsPlayed { get; init; }

            [Display(Name = "Largest Gain")]
            [DisplayFormat(NullDisplayText = "-")]
            public int? LargestGain { get; init; }

            [Display(Name = "Largest Loss")]
            [DisplayFormat(NullDisplayText = "-")]
            public int? LargestLoss { get; init; }
        }
    }
}
