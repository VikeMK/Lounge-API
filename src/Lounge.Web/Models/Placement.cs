using System;

namespace Lounge.Web.Models
{
    public class Placement
    {
        public int Id { get; set; }
        public int Season { get; set; } = 4;
        public DateTime AwardedOn { get; set; }
        public int Mmr { get; set; }
        public int? PrevMmr { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; } = default!;
    }
}
