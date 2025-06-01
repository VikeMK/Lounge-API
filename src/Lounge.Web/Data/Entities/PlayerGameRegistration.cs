using System;
using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Data.Entities
{
    public class PlayerGameRegistration
    {
        public int PlayerId { get; set; }
        public int Game { get; set; } = (int)Models.Enums.Game.mk8dx;
        public DateTime RegisteredOn { get; set; }
        public Player Player { get; set; } = default!;

        [Timestamp]
        public byte[] Timestamp { get; set; } = default!;
    }
}
