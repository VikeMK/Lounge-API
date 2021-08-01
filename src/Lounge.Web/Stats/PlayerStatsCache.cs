using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Lounge.Web.Stats
{
    public class PlayerStatsCache : IPlayerStatCache
    {
        // raw unranked stats
        private ConcurrentDictionary<int, Dictionary<int, PlayerStat>> _rawStats = new();

        // ranked stats
        private Dictionary<int, IReadOnlyList<RankedPlayerStat>> _sortedStats = new();
        private Dictionary<int, IReadOnlyDictionary<int, RankedPlayerStat>> _statsDict = new();

        public IReadOnlyList<RankedPlayerStat> GetAllStats(int season) => _sortedStats.GetValueOrDefault(season) ?? Array.Empty<RankedPlayerStat>();

        public bool TryGetPlayerStatsById(int id, int season, [NotNullWhen(true)] out RankedPlayerStat? playerStat)
        {
            playerStat = null;
            return _statsDict.TryGetValue(season, out var seasonStatsDict) && seasonStatsDict.TryGetValue(id, out playerStat);
        }

        public void UpdateAllPlayerStats(IReadOnlyList<PlayerStat> playerStats, int season)
        {
            _rawStats[season] = playerStats.ToDictionary(s => s.Id);
            UpdateRanks(season);
        }

        public void UpdatePlayerStats(PlayerStat playerStat, int season)
        {
            var rawSeasonStats = _rawStats.GetOrAdd(season, _ => new());
            rawSeasonStats[playerStat.Id] = playerStat;
            UpdateRanks(season);
        }

        private void UpdateRanks(int season)
        {
            var seasonRawStats = _rawStats[season];
            var statsDict = new Dictionary<int, RankedPlayerStat>(seasonRawStats.Count);
            var sortedStats = new List<RankedPlayerStat>(seasonRawStats.Count);

            var sortedUnrankedStats = seasonRawStats.Values
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

            _statsDict[season] = statsDict;
            _sortedStats[season] = sortedStats;
        }
    }
}
