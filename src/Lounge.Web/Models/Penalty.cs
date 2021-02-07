using System;

namespace Lounge.Web.Models
{
    public class Penalty
    {
        public int Id { get; set; }
        public DateTime AwardedOn { get; set; }
        public bool IsStrike { get; set; }
        public int PrevMmr { get; set; }
        public int NewMmr { get; set; }
        public DateTime? DeletedOn { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; } = default!;
    }
}
