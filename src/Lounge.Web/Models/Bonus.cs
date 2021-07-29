using System;

namespace Lounge.Web.Models
{
    public class Bonus
    {
        public int Id { get; set; }
        public int Season { get; set; } = 4;
        public DateTime AwardedOn { get; set; }
        public int PrevMmr { get; set; }
        public int NewMmr { get; set; }
        public DateTime? DeletedOn { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; } = default!;
    }
}
