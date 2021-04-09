using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lounge.Web.Stats
{
    public interface IPlayerStatCache
    {
        public bool TryGetPlayerStatsById(int id, [NotNullWhen(returnValue: true)]out RankedPlayerStat? playerStat);
        public IReadOnlyList<RankedPlayerStat> GetAllStats();
        public void UpdatePlayerStats(PlayerStat playerStat);
        public void UpdateAllPlayerStats(IReadOnlyList<PlayerStat> playerStats);
    }
}
