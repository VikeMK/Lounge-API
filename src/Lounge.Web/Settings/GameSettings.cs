using System.Collections.Generic;

namespace Lounge.Web.Settings
{
    public class GameSettings
    {
        public int CurrentSeason { get; set; }
        public required IReadOnlyDictionary<string, SeasonSettings> Seasons { get; set; }
    }
}
