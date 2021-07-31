﻿using System;
using System.Collections.Generic;

namespace Lounge.Web.Models
{
    public class Table
    {
        public int Id { get; set; }
        public int Season { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? VerifiedOn { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int NumTeams { get; set; }
        public string Url { get; set; } = default!;
        public string Tier { get; set; } = default!;
        public string? TableMessageId { get; set; }
        public string? UpdateMessageId { get; set; }
        public string? AuthorId { get; set; }

        public ICollection<TableScore> Scores { get; set; } = default!;
    }
}
