using System;

namespace Lounge.Web.Models
{
    public class Penalty
    {
        public int Id { get; set; }
        public DateTime AwardedOn { get; set; }
        public bool IsStrike { get; set; }
        public int PrevMMR { get; set; }
        public int NewMMR { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; } = default!;
    }
}
