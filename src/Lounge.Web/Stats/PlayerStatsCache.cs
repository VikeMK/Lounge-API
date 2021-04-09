using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Lounge.Web.Stats
{
    public class PlayerStatsCache : IPlayerStatCache
    {
        // raw unranked stats
        private Dictionary<int, PlayerStat> _rawStats = new();

        // ranked stats
        private IReadOnlyList<RankedPlayerStat> _sortedStats = Array.Empty<RankedPlayerStat>();
        private IReadOnlyDictionary<int, RankedPlayerStat> _statsDict = ImmutableDictionary.Create<int, RankedPlayerStat>();

        public IReadOnlyList<RankedPlayerStat> GetAllStats() => _sortedStats;

        public bool TryGetPlayerStatsById(int id, [NotNullWhen(true)] out RankedPlayerStat? playerStat)
            => _statsDict.TryGetValue(id, out playerStat);

        public void UpdateAllPlayerStats(IReadOnlyList<PlayerStat> playerStats)
        {
            _rawStats = playerStats.ToDictionary(s => s.Id);
            UpdateRanks();
        }

        public void UpdatePlayerStats(PlayerStat playerStat)
        {
            _rawStats[playerStat.Id] = playerStat;
            UpdateRanks();
        }

        private void UpdateRanks()
        {
            var statsDict = new Dictionary<int, RankedPlayerStat>(_rawStats.Count);
            var sortedStats = new List<RankedPlayerStat>(_rawStats.Count);

            var sortedUnrankedStats = _rawStats.Values
                .OrderByDescending(s => s.EventsPlayed > 0)
                .ThenByDescending(s => s.Mmr)
                .ThenBy(s => s.Name);

            int prev = -1;
            int? prevMmr = -1;
            int rank = 1;
            foreach (var stat in sortedUnrankedStats)
            {
                int? mmr = stat.Mmr;
                int actualRank = mmr == prevMmr ? prev : rank;
                var rankedStat = new RankedPlayerStat(actualRank, stat);

                statsDict[stat.Id] = rankedStat;
                sortedStats.Add(rankedStat);

                prev = actualRank;
                prevMmr = mmr;
                rank++;
            }

            _statsDict = statsDict;
            _sortedStats = sortedStats;
        }
    }
}
