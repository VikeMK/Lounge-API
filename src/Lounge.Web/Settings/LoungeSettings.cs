using System;
using System.Collections.Generic;

namespace Lounge.Web.Settings
{
    public class LoungeSettings
    {
        public int Season { get; set; }
        public IReadOnlyList<int> ValidSeasons { get; set; }
        public IReadOnlyDictionary<string, string> LeaderboardRefreshDelaysBySeason { get; set; }
    }
}
