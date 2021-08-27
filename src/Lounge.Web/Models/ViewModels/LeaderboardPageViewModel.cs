using System.Collections.Generic;

namespace Lounge.Web.Models.ViewModels
{
    public record LeaderboardPageViewModel(
        int Season,
        IReadOnlySet<string> ValidCountries);
}
