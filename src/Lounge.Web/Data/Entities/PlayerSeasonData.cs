using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Data.Entities
{
    public class PlayerSeasonData
    {
        public int Game { get; set; } = (int)Models.Enums.Game.mk8dx;
        public int Season { get; set; }
        public int Mmr { get; set; }
        public int? MaxMmr { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; } = default!;

        [Timestamp]
        public byte[] Timestamp { get; set; } = default!;
    }
}
