using Lounge.Web.Models.Enums;

namespace Lounge.Web.Utils;

public static class GameUtils
{
    /// <summary>
    /// Gets the display name for a season number based on the game.
    /// For mkworld, Season 0 is displayed as "Preseason".
    /// For other games, returns "Season {seasonNumber}".
    /// </summary>
    /// <param name="season">The season number</param>
    /// <returns>The display name for the season</returns>
    public static string GetSeasonDisplayName(int season)
    {
        return season == 0 ? "Preseason" : $"Season {season}";
    }
}
