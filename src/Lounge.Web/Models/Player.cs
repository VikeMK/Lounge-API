﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string NormalizedName { get; set; } = default!;
        public int MKCId { get; set; }
        public string? DiscordId { get; set; }

        public ICollection<PlayerSeasonData> SeasonData { get; set; } = default!;
        public ICollection<TableScore> TableScores { get; set; } = default!;
        public ICollection<Penalty> Penalties { get; set; } = default!;
        public ICollection<Bonus> Bonuses { get; set; } = default!;
        public ICollection<Placement> Placements { get; set; } = default !;

        [Timestamp]
        public byte[] Timestamp { get; set; } = default!;
    }
}
