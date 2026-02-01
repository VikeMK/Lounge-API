using Lounge.Web.Models.Enums;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lounge.Web.Stats
{
    public interface IPlayerStatCache
    {
        public bool TryGetPlayerStatsById(int id, GameMode game, int season, [NotNullWhen(returnValue: true)]out PlayerLeaderboardData? playerStat);
        public IReadOnlySet<string> GetAllCountryCodes(GameMode game, int season);
        public IReadOnlyList<PlayerLeaderboardData> GetAllStats(GameMode game, int season, LeaderboardSortOrder sortOrder = LeaderboardSortOrder.Mmr);
    }
}
