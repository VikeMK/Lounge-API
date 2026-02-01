using System;
using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Data.Entities
{
    public class PlayerGameRegistration
    {
        public int PlayerId { get; set; }
        public Models.Enums.RegistrationGameMode Game { get; set; } = Models.Enums.RegistrationGameMode.mk8dx;
        public DateTime RegisteredOn { get; set; }
        public Player Player { get; set; } = default!;

        [Timestamp]
        public byte[] Timestamp { get; set; } = default!;
    }
}
