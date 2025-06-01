using Lounge.Web.Models.Enums;
using System.Diagnostics.CodeAnalysis;

namespace Lounge.Web.Stats
{
    public interface IPlayerDetailsCache
    {
        bool TryGetPlayerDetailsById(int playerId, Game game, int season, [NotNullWhen(true)] out PlayerDetails? playerDetails);
        bool TryGetPlayerIdByName(string name, [NotNullWhen(true)] out int? playerId);
        bool TryGetPlayerIdByDiscord(string discord, [NotNullWhen(true)] out int? playerId);
        bool TryGetPlayerIdByFC(string fc, [NotNullWhen(true)] out int? playerId);
    }
}