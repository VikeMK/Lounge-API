using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Models.ViewModels
{
    public class PlayerDetailsViewModel
    {
        public PlayerDetailsViewModel(int playerId, string name, int mkcId, int? mmr, int? maxMmr, int overallRank, List<MmrChange> mmrChanges, int eventsPlayed, decimal? winRate, int winsLastTen, int lossesLastTen, int? gainLossLastTen, int? largestGain, int? largestLoss, double? averageScore, double? averageLastTen, double? partnerAverage)
        {
            PlayerId = playerId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MkcId = mkcId;
            Mmr = mmr;
            MaxMmr = maxMmr;
            MmrChanges = mmrChanges ?? throw new ArgumentNullException(nameof(mmrChanges));
            OverallRank = overallRank;
            EventsPlayed = eventsPlayed;
            WinRate = winRate;
            WinsLastTen = winsLastTen;
            LossesLastTen = lossesLastTen;
            GainLossLastTen = gainLossLastTen;
            LargestGain = largestGain;
            LargestLoss = largestLoss;
            AverageScore = averageScore;
            AverageLastTen = averageLastTen;
            PartnerAverage = partnerAverage;
        }

        public int PlayerId { get; set; }

        public string Name { get; set; }

        public int MkcId { get; set; }

        [Display(Name = "MMR")]
        [DisplayFormat(NullDisplayText = "Placement")]
        public int? Mmr { get; set; }

        [Display(Name = "Peak MMR")]
        [DisplayFormat(NullDisplayText = "N/A")]
        public int? MaxMmr { get; set; }

        [Display(Name = "Rank")]
        [DisplayFormat(NullDisplayText = "-")]
        public int? OverallRank { get; set; }

        [Display(Name = "Events Played")]
        public int EventsPlayed { get; set; }

        [Display(Name = "Win Rate")]
        [DisplayFormat(NullDisplayText = "N/A", DataFormatString = "{0:P1}")]
        public decimal? WinRate { get; set; }

        public int WinsLastTen { get; set; }

        public int LossesLastTen { get; set; }

        [Display(Name = "Win - Loss (Last 10)")]
        public string WinLossLastTen => $"{WinsLastTen} - {LossesLastTen}";

        [Display(Name = "Gain/Loss (Last 10)")]
        [DisplayFormat(NullDisplayText = "N/A", DataFormatString = "{0:+#;-#;0}")]
        public int? GainLossLastTen { get; set; }

        [Display(Name = "Largest Gain")]
        [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:+#;-#;0}")]
        public int? LargestGain { get; set; }

        [Display(Name = "Largest Loss")]
        [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:+#;-#;0}")]
        public int? LargestLoss { get; set; }

        [Display(Name = "Average Score")]
        [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
        public double? AverageScore { get; set; }

        [Display(Name = "Average Score (Last 10)")]
        [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
        public double? AverageLastTen { get; set; }

        [Display(Name = "Partner Average Score")]
        [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
        public double? PartnerAverage { get; set; }

        public List<MmrChange> MmrChanges { get; set; }

        [Display(Name = "Forum Link")]
        public string ForumLink => $"https://www.mariokartcentral.com/forums/index.php?members/{MkcId}/";

        public class MmrChange
        {
            public MmrChange(int? changeId, int newMmr, int mmrDelta, MmrChangeReason reason, DateTime time, int? score = null, IReadOnlyList<int>? partnerScores = null, int? rank = null)
            {
                ChangeId = changeId;
                NewMmr = newMmr;
                MmrDelta = mmrDelta;
                Reason = reason;
                Time = time;
                Score = score;
                PartnerScores = partnerScores;
                Rank = rank;
            }

            public int? ChangeId { get; set; }

            [Display(Name = "MMR")]
            public int NewMmr { get; set; }

            [Display(Name = "MMR Delta")]
            [DisplayFormat(DataFormatString = "{0:+#;-#;0}")]
            public int MmrDelta { get; set; }

            public MmrChangeReason Reason { get; set; }

            public DateTime Time { get; set; }
            public int? Score { get; set; }
            public IReadOnlyList<int>? PartnerScores { get; set; }
            public int? Rank { get; set; }
        }

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
