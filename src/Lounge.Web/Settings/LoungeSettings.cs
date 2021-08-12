using System.Collections.Generic;

namespace Lounge.Web.Settings
{
    public class LoungeSettings
    {
        public int CurrentSeason { get; set; }
        public IReadOnlyDictionary<string, SeasonSettings> Seasons { get; set; }
        public IReadOnlyDictionary<string, string> CountryNames { get; set; }
    }
}
