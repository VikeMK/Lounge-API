using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Data.Entities
{
    public class Table
    {
        public int Id { get; set; }
        public int Game { get; set; } = (int)Models.Enums.Game.mk8dx;
        public int Season { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? VerifiedOn { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int NumTeams { get; set; }
        public string Tier { get; set; } = default!;
        public string? TableMessageId { get; set; }
        public string? UpdateMessageId { get; set; }
        public string? AuthorId { get; set; }

        public ICollection<TableScore> Scores { get; set; } = default!;

        [Timestamp]
        public byte[] Timestamp { get; set; } = default!;
    }
}
