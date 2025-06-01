using System.Collections.Generic;

namespace Lounge.Web.Settings
{
    public class LoungeSettings
    {
        public required IReadOnlyDictionary<string, GameSettings> Games { get; set; }
        public required IReadOnlyDictionary<string, string> CountryNames { get; set; }
    }
}
