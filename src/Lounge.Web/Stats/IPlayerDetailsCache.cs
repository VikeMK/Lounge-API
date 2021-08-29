using System.Diagnostics.CodeAnalysis;

namespace Lounge.Web.Stats
{
    public interface IPlayerDetailsCache
    {
        bool TryGetPlayerDetailsById(int playerId, int season, [NotNullWhen(true)] out PlayerDetails? playerDetails);
        bool TryGetPlayerIdByName(string name, [NotNullWhen(true)] out int? playerId);
    }
}