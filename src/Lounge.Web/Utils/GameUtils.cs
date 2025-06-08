using Lounge.Web.Models.Enums;

namespace Lounge.Web.Utils;

public static class GameUtils
{
    /// <summary>
    /// Gets the display name for a season number based on the game.
    /// For mkworld, Season 0 is displayed as "Preseason".
    /// For other games, returns "Season {seasonNumber}".
    /// </summary>
    /// <param name="game">The game</param>
    /// <param name="seasonNumber">The season number</param>
    /// <returns>The display name for the season</returns>
    public static string GetSeasonDisplayName(Game game, int seasonNumber)
    {
        if (seasonNumber == 0)
        {
            return "Preseason";
        }
        return $"Season {seasonNumber}";
    }
}
