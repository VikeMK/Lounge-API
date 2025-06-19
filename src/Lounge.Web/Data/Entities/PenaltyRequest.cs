﻿using System;

namespace Lounge.Web.Data.Entities
{
    public class PenaltyRequest
    {
        public int Id { get; set; }
        public int Game { get; set; } = (int)Models.Enums.Game.mk8dx;
        public string PenaltyName { get; set; } = default!;
        public int TableId { get; set; }
        public Table Table { get; set; } = default!;
        public int NumberOfRaces { get; set; }

        public int? ReporterId {  get; set; }
        public Player? Reporter { get; set; } = default!;

        public int PlayerId { get; set; }
        public Player Player { get; set; } = default!;
    }
}
