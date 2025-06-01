﻿namespace Lounge.Web.Data.Entities.ChangeTracking
{
    public class  PlayerGameRegistrationChange : Change<PlayerGameRegistration>
    {
        public int PlayerId { get; set; }
        public int Game { get; set; }
    }
}
