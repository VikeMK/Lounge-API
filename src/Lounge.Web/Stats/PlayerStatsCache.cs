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
        private Dictionary<int, IReadOnlyDictionary<LeaderboardSortOrder, IReadOnlyList<RankedPlayerStat>>> _sortedStats = new();
        private Dictionary<int, IReadOnlySet<string>> _countryCodes = new();
        private Dictionary<int, IReadOnlyDictionary<int, RankedPlayerStat>> _statsDict = new();

        public IReadOnlyList<RankedPlayerStat> GetAllStats(int season, LeaderboardSortOrder sortOrder = LeaderboardSortOrder.Mmr) => 
            _sortedStats.TryGetValue(season, out var statsLookup)
                ? (statsLookup.GetValueOrDefault(sortOrder) ?? statsLookup.GetValueOrDefault(LeaderboardSortOrder.Mmr) ?? Array.Empty<RankedPlayerStat>())
                : Array.Empty<RankedPlayerStat>();

        public IReadOnlySet<string> GetAllCountryCodes(int season)
        {
            return _countryCodes.GetValueOrDefault(season) ?? ImmutableHashSet.Create<string>();
        }

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
            var countryCodes = new HashSet<string>();

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
                if (stat.CountryCode != null)
                    countryCodes.Add(stat.CountryCode);

                prev = actualRank;
                prevMmr = mmr;
                rank++;
            }

            _statsDict[season] = statsDict;
            _countryCodes[season] = countryCodes;
            _sortedStats[season] = new Dictionary<LeaderboardSortOrder, IReadOnlyList<RankedPlayerStat>>
            {
                [LeaderboardSortOrder.Mmr] = sortedStats,
                [LeaderboardSortOrder.MaxMmr] = sortedStats.OrderByDescending(s => s.Stat.MaxMmr).ThenBy(s => s.Rank).ToList(),
                [LeaderboardSortOrder.EventsPlayed] = sortedStats.OrderByDescending(s => s.Stat.EventsPlayed).ThenBy(s => s.Rank).ToList(),
                [LeaderboardSortOrder.Name] = sortedStats.OrderBy(s => s.Stat.Name).ToList(),
                [LeaderboardSortOrder.LargestGain] = sortedStats.OrderByDescending(s => s.Stat.LargestGain ?? int.MinValue).ThenBy(s => s.Rank).ToList(),
                [LeaderboardSortOrder.LargestLoss] = sortedStats.OrderBy(s => s.Stat.LargestLoss ?? int.MaxValue).ThenBy(s => s.Rank).ToList(),
                [LeaderboardSortOrder.WinRate] = sortedStats.OrderByDescending(s => s.Stat.EventsPlayed == 0 ? int.MinValue : ((decimal)s.Stat.Wins / s.Stat.EventsPlayed)).ThenByDescending(s => s.Stat.EventsPlayed).ThenBy(s => s.Rank).ToList()
            };
        }
    }
}
