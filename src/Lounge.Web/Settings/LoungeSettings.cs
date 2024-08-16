using System.Collections.Generic;

namespace Lounge.Web.Settings
{
    public class LoungeSettings
    {
        public int CurrentSeason { get; set; }
        public required IReadOnlyDictionary<string, SeasonSettings> Seasons { get; set; }
        public required IReadOnlyDictionary<string, string> CountryNames { get; set; }
    }
}
