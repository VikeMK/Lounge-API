using System;
using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Data.Entities
{
    public class NameChange
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string NormalizedName { get; set; } = default!;

        public int Season { get; set; }
        public DateTime ChangedOn { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; } = default!;

        [Timestamp]
        public byte[] Timestamp { get; set; } = default!;
    }
}
