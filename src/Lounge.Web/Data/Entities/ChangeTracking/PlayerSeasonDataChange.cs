﻿namespace Lounge.Web.Data.Entities.ChangeTracking
{
    public class PlayerSeasonDataChange : Change<PlayerSeasonData>
    {
        public int Season { get; set; }
        public int PlayerId { get; set; }
    }
}
