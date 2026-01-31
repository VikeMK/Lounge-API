using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Data.Entities
{
    public class PlayerSeasonData
    {
        public Models.Enums.GameMode Game { get; set; } = Models.Enums.GameMode.mk8dx;
        public int Season { get; set; }
        public int Mmr { get; set; }
        public int? MaxMmr { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; } = default!;

        [Timestamp]
        public byte[] Timestamp { get; set; } = default!;
    }
}
