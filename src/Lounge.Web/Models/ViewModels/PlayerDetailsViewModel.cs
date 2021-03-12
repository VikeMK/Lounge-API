using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Models.ViewModels
{
    public class PlayerDetailsViewModel
    {
        public PlayerDetailsViewModel(int playerId, string name, int mkcId, int? mmr, int? maxMmr, int overallRank, List<MmrChange> mmrChanges, int eventsPlayed, decimal? winRate, int winsLastTen, int lossesLastTen, int? gainLossLastTen, int? largestGain, int? largestLoss)
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
        }

        public int PlayerId { get; set; }

        public string Name { get; set; }

        public int MkcId { get; set; }

        [Display(Name = "MMR")]
        [DisplayFormat(NullDisplayText = "Placement")]
        public int? Mmr { get; set; }

        [Display(Name = "Peak MMR")]
        [DisplayFormat(NullDisplayText = "Placement")]
        public int? MaxMmr { get; set; }

        public int? OverallRank { get; set; }

        public int EventsPlayed { get; set; }

        public decimal? WinRate { get; set; }

        public int WinsLastTen { get; set; }

        public int LossesLastTen { get; set; }

        public int? GainLossLastTen { get; set; }

        public int? LargestGain { get; set; }

        public int? LargestLoss { get; set; }

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
