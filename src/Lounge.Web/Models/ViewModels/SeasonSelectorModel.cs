using Lounge.Web.Models.Enums;
using System.Collections.Generic;

namespace Lounge.Web.Models.ViewModels
{
    public class SeasonSelectorModel
    {
        public string PageName { get; set; } = string.Empty;
        public Game Game { get; set; }
        public int? PlayerId { get; set; } // Only used for PlayerDetails
        public int CurrentSeason { get; set; }
        public IReadOnlyList<int> ValidSeasons { get; set; } = new List<int>();
        public Dictionary<string, object>? ExtraRouteValues { get; set; } // For additional routing needs
    }
}
