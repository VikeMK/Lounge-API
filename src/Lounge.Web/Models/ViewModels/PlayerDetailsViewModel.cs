﻿using Lounge.Web.Models.Enums;
using Lounge.Web.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Models.ViewModels
{

    public class PlayerDetailsViewModel
    {
        public int PlayerId { get; init; }

        public string Name { get; init; }

        public int MkcId { get; init; }

        public int Season { get; init; }

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

        public int? LargestGainTableId { get; init; }

        [Display(Name = "Largest Loss")]
        [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:+#;-#;0}")]
        public int? LargestLoss { get; init; }

        public int? LargestLossTableId { get; init; }

        [Display(Name = "Average Score")]
        [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
        public double? AverageScore { get; init; }

        [Display(Name = "Average Score (Last 10)")]
        [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
        public double? AverageLastTen { get; init; }

        [Display(Name = "Partner Average Score")]
        [DisplayFormat(NullDisplayText = "-", DataFormatString = "{0:F1}")]
        public double? PartnerAverage { get; init; }

        public List<MmrChange> MmrChanges { get; init; }

        [Display(Name = "Rank")]
        public Rank Rank => RankUtils.GetRank(Mmr);

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
