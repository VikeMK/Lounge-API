using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lounge.Web.Stats
{
    public interface IPlayerStatCache
    {
        public bool TryGetPlayerStatsById(int id, int season, [NotNullWhen(returnValue: true)]out PlayerEventHistory? playerStat);
        public IReadOnlySet<string> GetAllCountryCodes(int season);
        public IReadOnlyList<PlayerEventHistory> GetAllStats(int season, LeaderboardSortOrder sortOrder = LeaderboardSortOrder.Mmr);
    }
}
