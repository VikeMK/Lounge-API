using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lounge.Web.Stats
{
    public interface IPlayerStatCache
    {
        public bool TryGetPlayerStatsById(int id, int season, [NotNullWhen(returnValue: true)]out RankedPlayerStat? playerStat);
        public IReadOnlyList<RankedPlayerStat> GetAllStats(int season);
        public void UpdatePlayerStats(PlayerStat playerStat, int season);
        public void UpdateAllPlayerStats(IReadOnlyList<PlayerStat> playerStats, int season);
    }
}
