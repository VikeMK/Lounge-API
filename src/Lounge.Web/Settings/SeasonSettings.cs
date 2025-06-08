using System;
using System.Collections.Generic;

namespace Lounge.Web.Settings
{
    public class SeasonSettings
    {
        public double SquadQueueMultiplier { get; set; }
        public TimeSpan LeaderboardRefreshDelay { get; set; }
        public required IReadOnlyDictionary<string, int> Ranks { get; set; }
        public required IReadOnlyList<string> RecordsTierOrder { get; set; }
        public required IReadOnlyDictionary<string, IReadOnlyList<string>> DivisionsToTier { get; set; }
    }
}
