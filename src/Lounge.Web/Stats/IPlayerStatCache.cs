using Lounge.Web.Models.Enums;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lounge.Web.Stats
{
    public interface IPlayerStatCache
    {
        public bool TryGetPlayerStatsById(int id, Game game, int season, [NotNullWhen(returnValue: true)]out PlayerLeaderboardData? playerStat);
        public IReadOnlySet<string> GetAllCountryCodes(Game game, int season);
        public IReadOnlyList<PlayerLeaderboardData> GetAllStats(Game game, int season, LeaderboardSortOrder sortOrder = LeaderboardSortOrder.Mmr);
    }
}
