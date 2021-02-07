using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Models.ViewModels
{
    public class PlayerDetailsViewModel
    {
        public int PlayerId { get; set; }

        public string Name { get; set; }

        public int MkcId { get; set; }

        [Display(Name = "MMR")]
        [DisplayFormat(NullDisplayText = "Placement")]
        public int? Mmr { get; set; }

        [Display(Name = "Peak MMR")]
        [DisplayFormat(NullDisplayText = "Placement")]
        public int? MaxMmr { get; set; }

        public List<MmrChange> MmrChanges { get; set; }

        [Display(Name = "Forum Link")]
        public string ForumLink => $"https://www.mariokartcentral.com/forums/index.php?members/{MkcId}/";

        public class MmrChange
        {
            public int? ChangeId { get; set; }

            [Display(Name = "MMR")]
            public int NewMMR { get; set; }

            [Display(Name = "MMR Delta")]
            public int MmrDelta { get; set; }

            public MmrChangeReason Reason { get; set; }

            public DateTime Time { get; set; }
        }

        public enum MmrChangeReason
        {
            Placement,
            Table,
            Penalty,

            [Display(Name = "Deleted Table")]
            TableDelete,

            [Display(Name = "Deleted Penalty")]
            PenaltyDelete
        }
    }
}
