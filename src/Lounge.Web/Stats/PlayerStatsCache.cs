using Lounge.Web.Data.ChangeTracking;
using Lounge.Web.Models.Enums;
using Lounge.Web.Settings;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Lounge.Web.Stats
{
    public class PlayerStatsCache : IPlayerStatCache, IDbCacheUpdateSubscriber
    {
        record SeasonStatsData(
            IReadOnlySet<string> CountryCodes,
            IReadOnlyDictionary<int, PlayerLeaderboardData> Players,
            Dictionary<LeaderboardSortOrder, IReadOnlyList<PlayerLeaderboardData>> PlayerSortOrders);

        private readonly ILoungeSettingsService _loungeSettingsService;

        private IReadOnlyDictionary<(Game Game, int Season), SeasonStatsData> _seasonStats = new Dictionary<(Game Game, int Season), SeasonStatsData>();

        public PlayerStatsCache(ILoungeSettingsService loungeSettingsService)
        {
            _loungeSettingsService = loungeSettingsService;
        }

        public IReadOnlyList<PlayerLeaderboardData> GetAllStats(Game game, int season, LeaderboardSortOrder sortOrder = LeaderboardSortOrder.Mmr)
        {
            if (!_seasonStats.TryGetValue((game, season), out var seasonStats))
                return Array.Empty<PlayerLeaderboardData>();

            if (seasonStats.PlayerSortOrders.TryGetValue(sortOrder, out var sortedStats))
                return sortedStats;

            sortedStats = this.GetSortedPlayerData(seasonStats.Players.Values, sortOrder);
            seasonStats.PlayerSortOrders[sortOrder] = sortedStats;
            return sortedStats;
        }

        public IReadOnlySet<string> GetAllCountryCodes(Game game, int season)
        {
            return _seasonStats.TryGetValue((game, season), out var seasonData) ? seasonData.CountryCodes : ImmutableHashSet<string>.Empty;
        }

        public bool TryGetPlayerStatsById(int id, Game game, int season, [NotNullWhen(true)] out PlayerLeaderboardData? playerStat)
        {
            playerStat = null;
            return _seasonStats.TryGetValue((game, season), out var seasonStats) && seasonStats.Players.TryGetValue(id, out playerStat);
        }

        public void OnChange(IDbCache dbCache)
        {
            var newSeasonStats = new Dictionary<(Game Game, int Season), SeasonStatsData>();
            var countryNames = _loungeSettingsService.CountryNames;
            foreach (var game in _loungeSettingsService.ValidGames)
            {
                var seasons = _loungeSettingsService.ValidSeasons[game];
                var registrations = dbCache.PlayerGameRegistrations[game];

                foreach (var season in seasons)
                {
                    var key = (game, season);
                    var seasonData = dbCache.PlayerSeasonData.GetValueOrDefault(key);

                    var sqMultiplier = _loungeSettingsService.SquadQueueMultipliers[game][season];
                    var playerLookup = new Dictionary<int, PlayerLeaderboardData>();
                    var playerEventsLookup = new Dictionary<int, List<PlayerEventData>>();
                    var countryCodes = new HashSet<string>();
                    foreach (var playerId in registrations.Keys)
                    {
                        var player = dbCache.Players[playerId];
                        var psd = seasonData?.GetValueOrDefault(playerId);
                        var eventsList = new List<PlayerEventData>();
                        playerEventsLookup[playerId] = eventsList;
                        playerLookup[playerId] = new(playerId, player.Name, player.RegistryId ?? -1, player.DiscordId, player.RegistryId, player.CountryCode, player.SwitchFc, player.IsHidden, psd?.Mmr, psd?.MaxMmr, eventsList);
                        if (player.CountryCode is string countryCode && countryNames.ContainsKey(countryCode))
                            countryCodes.Add(countryCode);
                    }

                    var tablesLookup = new Dictionary<int, EventData>();
                    foreach (var table in dbCache.Tables.Values.Where(v => v.Season == season && v.Game == (int)game && v.VerifiedOn != null && v.DeletedOn == null))
                    {
                        var eventData = new EventData(table.Id, table.NumTeams, table.Tier, table.VerifiedOn!.Value);
                        tablesLookup[table.Id] = eventData;

                        var formatMultiplier = string.Equals(table.Tier, "SQ", StringComparison.OrdinalIgnoreCase) ? sqMultiplier : 1;

                        var tableScores = dbCache.TableScores.GetValueOrDefault(table.Id)?.Values?.ToList();
                        if (tableScores is null)
                            continue;

                        foreach (var tableScore in tableScores)
                        {
                            var multiplier = tableScore.Multiplier * formatMultiplier;
                            var mmrDelta = tableScore.NewMmr!.Value - tableScore.PrevMmr!.Value;
                            var partnerScores = tableScores.Where(s => s.Team == tableScore.Team && s.PlayerId != tableScore.PlayerId).Select(s => s.Score).ToList();
                            var playerEventData = new PlayerEventData(tableScore.TableId, tableScore.Score, tableScore.Multiplier, mmrDelta, partnerScores, eventData);
                            playerEventsLookup[tableScore.PlayerId].Add(playerEventData);
                        }
                    }

                    // sort all the event lists and update all the player lookups
                    foreach (var playerId in playerLookup.Keys)
                    {
                        var sortedEventsList = playerEventsLookup[playerId].OrderByDescending(e => tablesLookup[e.TableId].VerifiedOn).ToList();
                        playerLookup[playerId] = playerLookup[playerId].WithUpdatedEvents(sortedEventsList);
                    }

                    var playersSortedByMmr = playerLookup.Values
                        .OrderByDescending(s => s.HasEvents)
                        .ThenByDescending(s => s.Mmr)
                        .ThenBy(s => s.Name);

                    int prev = -1;
                    int? prevMmr = -1;
                    int rank = 1;
                    foreach (var playerData in playersSortedByMmr)
                    {
                        int? mmr = playerData.Mmr;
                        int actualRank = mmr == prevMmr ? prev : rank;
                        playerLookup[playerData.Id] = playerData with { OverallRank = actualRank };

                        if (playerData.IsHidden)
                            continue;

                        prev = actualRank;
                        prevMmr = mmr;
                        rank++;
                    }

                    newSeasonStats[key] = new SeasonStatsData(countryCodes, playerLookup, new Dictionary<LeaderboardSortOrder, IReadOnlyList<PlayerLeaderboardData>>());
                }
            }

            _seasonStats = newSeasonStats;
        }

        private IReadOnlyList<PlayerLeaderboardData> GetSortedPlayerData(IEnumerable<PlayerLeaderboardData> playerData, LeaderboardSortOrder sortOrder)
        {
            // filter out hidden players
            playerData = playerData.Where(p => !p.IsHidden);

            playerData = sortOrder switch
            {
                LeaderboardSortOrder.Name => playerData.OrderBy(p => p.Name),
                LeaderboardSortOrder.Mmr => playerData.OrderBy(p => p.OverallRank).ThenBy(p => p.Name),
                LeaderboardSortOrder.MaxMmr => playerData.OrderByDescending(p => p.MaxMmr).ThenBy(p => p.OverallRank).ThenBy(s => s.Name),
                LeaderboardSortOrder.WinRate => playerData
                    .OrderByDescending(s => s.HasEvents)
                    .ThenByDescending(s => s.WinRate)
                    .ThenByDescending(s => s.EventsPlayed)
                    .ThenBy(s => s.OverallRank)
                    .ThenBy(s => s.Name),
                LeaderboardSortOrder.WinLossLast10 => playerData
                    .OrderByDescending(s => s.HasEvents)
                    .ThenByDescending(s => s.LastTenWins)
                    .ThenBy(s => s.LastTenLosses)
                    .ThenBy(s => s.OverallRank)
                    .ThenBy(s => s.Name),
                LeaderboardSortOrder.GainLast10 => playerData
                    .OrderByDescending(s => s.HasEvents)
                    .ThenByDescending(s => s.LastTenGainLoss)
                    .ThenBy(s => s.OverallRank)
                    .ThenBy(s => s.Name),
                LeaderboardSortOrder.EventsPlayed => playerData
                    .OrderByDescending(s => s.EventsPlayed)
                    .ThenBy(s => s.OverallRank)
                    .ThenBy(s => s.Name),
                LeaderboardSortOrder.AverageScoreNoSQ => playerData
                    .OrderByDescending(s => s.HasEvents)
                    .ThenByDescending(s => s.NoSQAverageScore)
                    .ThenBy(s => s.OverallRank)
                    .ThenBy(s => s.Name),
                LeaderboardSortOrder.AverageScoreNoSQLast10 => playerData
                    .OrderByDescending(s => s.HasEvents)
                    .ThenByDescending(s => s.NoSQAverageLastTen)
                    .ThenBy(s => s.OverallRank)
                    .ThenBy(s => s.Name),
                _ => playerData.OrderBy(p => p.OverallRank).ThenBy(p => p.Name)
            };

            return playerData.ToList();
        }
    }
}
