using Lounge.Web.Models.Enums;
using Lounge.Web.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;

namespace Lounge.Web.Models.ViewModels
{
    public class PlayerDetailsViewModel
    {
        public int PlayerId { get; init; }

        public required string Name { get; init; }

        public int MkcId { get; init; }

        public int? RegistryId { get; init; }

        public string? CountryCode { get; init; }

        [Display(Name = "Country")]
        [DisplayFormat(NullDisplayText = "Unknown")]
        public string? CountryName { get; init; }

        public string? SwitchFc { get; init; }

        public bool IsHidden { get; init; }

        public Game Game { get; init; }

        public int Season { get; init; }

        [JsonIgnore]
        public string SeasonDisplayName => GameUtils.GetSeasonDisplayName(Game, Season);

        [Display(Name = "MMR")]
        [DisplayFormat(NullDisplayText = "Placement")]
        public int? Mmr { get; init; }

        [Display(Name = "Peak MMR")]
        [DisplayFormat(NullDisplayText = "N/A")]
        public int? MaxMmr { get; init; }

        [Display(Name = "Rank")]
        [DisplayFormat(NullDisplayText = "-")]
        public int? OverallRank { get; init; }

        [Display(Name = "Events Played")]
        public int EventsPlayed { get; init; }

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

        [Display(Name = "Largest Gain")]
        [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:+#;-#;0}")]
        public int? LargestGain { get; init; }

        // DEPRECATED - No Longer Set
        public int? LargestLoss { get; init; }

        // DEPRECATED - No Longer Set
        public int? LargestLossTableId { get; init; }

        public int? LargestGainTableId { get; init; }

        [Display(Name = "Average Score")]
        [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
        public double? AverageScore { get; init; }

        [Display(Name = "Average Score (No SQ)")]
        [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
        public double? NoSQAverageScore { get; init; }

        [Display(Name = "Average Score (Last 10)")]
        [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
        public double? AverageLastTen { get; init; }

        [Display(Name = "Average Score (No SQ, Last 10)")]
        [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
        public double? NoSQAverageLastTen { get; init; }

        [Display(Name = "Partner Average Score")]
        [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
        public double? PartnerAverage { get; init; }

        [Display(Name = "Partner Average Score (No SQ)")]
        [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
        public double? NoSQPartnerAverage { get; init; }

        public required List<MmrChange> MmrChanges { get; init; }

        public required List<NameChange> NameHistory { get; init; }

        [JsonIgnore]
        public Rank RankData { get; init; }

        public string Rank => RankData!.Name;

        [Display(Name = "Registry Link")]
        public string? RegistryLink =>
            RegistryId != null ? $"https://mkcentral.com/registry/players/profile?id={RegistryId}" : null;

        [JsonIgnore]
        public IReadOnlyList<int>? ValidSeasons { get; set; }

        // Computes per-room-size statistics from table events in MmrChanges
        public Dictionary<int, RoomSizeLeaderboardData> GetRoomSizeStats()
        {
            // Ensure descending by time like other stats expect
            var tableEvents = MmrChanges
                .Where(c => c.Reason == MmrChangeReason.Table && c.NumPlayers.HasValue)
                .OrderByDescending(c => c.Time)
                .ToList();

            var result = tableEvents
                .GroupBy(e => e.NumPlayers!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => new RoomSizeLeaderboardData
                    {
                        EventsPlayed = g.Count(),
                        WinRate = g.Any() ? (decimal)g.Count(e => e.MmrDelta > 0) / g.Count() : null,
                        LastTenWins = g.Take(10).Count(e => e.MmrDelta > 0),
                        LastTenLosses = g.Take(10).Count(e => e.MmrDelta <= 0),
                        LastTenGainLoss = g.Take(10).Sum(e => e.MmrDelta),
                        LargestGain = g.Where(e => e.MmrDelta > 0).Max(e => ((int, int)?)(e.MmrDelta, e.ChangeId!.Value)),
                        AverageScore = g.Average(e => (int?)e.Score),
                        NoSQAverageScore = g.Where(e => !string.Equals(e.Tier, "SQ", StringComparison.OrdinalIgnoreCase)).Average(e => (int?)e.Score),
                        AverageLastTen = g.Take(10).Average(e => (int?)e.Score),
                        NoSQAverageLastTen = g.Where(e => !string.Equals(e.Tier, "SQ", StringComparison.OrdinalIgnoreCase)).Take(10).Average(e => (int?)e.Score),
                        PartnerAverage = g.SelectMany(p => p.PartnerScores ?? Array.Empty<int>()).Cast<int?>().Average(),
                        NoSQPartnerAverage = g.Where(e => !string.Equals(e.Tier, "SQ", StringComparison.OrdinalIgnoreCase)).SelectMany(p => p.PartnerScores ?? Array.Empty<int>()).Cast<int?>().Average(),
                    }
                );

            return result;
        }

        public class RoomSizeLeaderboardData
        {
            [Display(Name = "Events Played")]
            public int EventsPlayed { get; init; }

            [Display(Name = "Win Rate")]
            [DisplayFormat(NullDisplayText = "N/A", DataFormatString = "{0:P1}")]
            public decimal? WinRate { get; init; }

            [Display(Name = "Wins (Last 10)")]
            public int LastTenWins { get; init; }

            [Display(Name = "Losses (Last 10)")]
            public int LastTenLosses { get; init; }

            [Display(Name = "Gain/Loss (Last 10)")]
            [DisplayFormat(NullDisplayText = "N/A", DataFormatString = "{0:+#;-#;0}")]
            public int LastTenGainLoss { get; init; }

            [Display(Name = "Largest Gain")]
            [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:+#;-#;0}")]
            public (int Amount, int EventId)? LargestGain { get; init; }

            [Display(Name = "Average Score")]
            [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
            public double? AverageScore { get; init; }

            [Display(Name = "Average Score (No SQ)")]
            [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
            public double? NoSQAverageScore { get; init; }

            [Display(Name = "Average Score (Last 10)")]
            [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
            public double? AverageLastTen { get; init; }

            [Display(Name = "Average Score (No SQ, Last 10)")]
            [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
            public double? NoSQAverageLastTen { get; init; }

            [Display(Name = "Partner Average Score")]
            [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
            public double? PartnerAverage { get; init; }

            [Display(Name = "Partner Average Score (No SQ)")]
            [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
            public double? NoSQPartnerAverage { get; init; }
        }

        public class MmrChange
        {
            public MmrChange(int? changeId, int newMmr, int mmrDelta, MmrChangeReason reason, DateTime time, int? score = null, IReadOnlyList<int>? partnerScores = null, IReadOnlyList<int>? partnerIds = null, int? rank = null, string? tier = null, int? numTeams = null, int? numPlayers = null)
            {
                ChangeId = changeId;
                NewMmr = newMmr;
                MmrDelta = mmrDelta;
                Reason = reason;
                Time = time;
                Score = score;
                PartnerScores = partnerScores;
                PartnerIds = partnerIds;
                Rank = rank;
                Tier = tier;
                NumTeams = numTeams;
                NumPlayers = numPlayers;
            }

            public int? ChangeId { get; set; }

            [Display(Name = "MMR")]
            public int NewMmr { get; set; }

            [Display(Name = "MMR Delta")]
            [DisplayFormat(DataFormatString = "{0:+#;-#;0}")]
            public int MmrDelta { get; set; }

            [Display(Name = "Event")]
            public MmrChangeReason Reason { get; set; }

            public DateTime Time { get; set; }
            public int? Score { get; set; }
            public IReadOnlyList<int>? PartnerScores { get; set; }
            public IReadOnlyList<int>? PartnerIds { get; set; }
            public int? Rank { get; set; }
            public string? Tier { get; set; }
            public int? NumTeams { get; set; }
            public int? NumPlayers { get; set; }
        }

        public record NameChange(string Name, DateTime ChangedOn);

        public enum MmrChangeReason
        {
            Placement,
            Table,
            Penalty,
            Strike,
            Bonus,

            [Display(Name = "Deleted Table")]
            TableDelete,

            [Display(Name = "Deleted Penalty")]
            PenaltyDelete,

            [Display(Name = "Deleted Strike")]
            StrikeDelete,

            [Display(Name = "Deleted Bonus")]
            BonusDelete
        }
    }
}
