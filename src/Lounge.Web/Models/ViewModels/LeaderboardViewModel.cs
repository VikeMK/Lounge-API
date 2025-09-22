using System;
using System.Collections.Generic;

namespace Lounge.Web.Models.ViewModels
{
    public class LeaderboardViewModel
    {
        public int TotalPlayers { get; init; }

        public required List<Player> Data { get; init; }

        public class Player
        {
            public int Id { get; set; }

            public int? OverallRank { get; init; }

            public string? CountryCode { get; init; }

            public required string Name { get; init; }

            public int? Mmr { get; init; }

            public int? MaxMmr { get; init; }

            public decimal? WinRate { get; init; }

            public int WinsLastTen { get; init; }

            public int LossesLastTen { get; init; }

            public int? GainLossLastTen { get; init; }

            public int? LastWeekRankChange { get; init; }

            public int EventsPlayed { get; init; }

            public int? LargestGain { get; init; }

            public Rank? MmrRank { get; init; }

            public Rank? MaxMmrRank { get; init; }

            public DateTime? AccountCreationDateUtc { get; init; }

            public double? AverageScore12P { get; init; }

            public double? AverageScore24P { get; init; }
        }
    }
}
