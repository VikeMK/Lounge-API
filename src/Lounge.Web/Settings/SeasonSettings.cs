using System;
using System.Collections.Generic;

namespace Lounge.Web.Settings
{
    public class SeasonSettings
    {
        public TimeSpan LeaderboardRefreshDelay { get; set; }
        public IReadOnlyDictionary<string, int> Ranks { get; set; }
    }
}
