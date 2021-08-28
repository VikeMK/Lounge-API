using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lounge.Web.Stats
{
    public interface IPlayerStatCache
    {
        public bool TryGetPlayerStatsById(int id, int season, [NotNullWhen(returnValue: true)]out PlayerLeaderboardData? playerStat);
        public IReadOnlySet<string> GetAllCountryCodes(int season);
        public IReadOnlyList<PlayerLeaderboardData> GetAllStats(int season, LeaderboardSortOrder sortOrder = LeaderboardSortOrder.Mmr);
    }
}
