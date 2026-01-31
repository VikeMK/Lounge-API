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

        private IReadOnlyDictionary<(GameMode Game, int Season), SeasonStatsData> _seasonStats = new Dictionary<(GameMode Game, int Season), SeasonStatsData>();

        public PlayerStatsCache(ILoungeSettingsService loungeSettingsService)
        {
            _loungeSettingsService = loungeSettingsService;
        }

        public IReadOnlyList<PlayerLeaderboardData> GetAllStats(GameMode game, int season, LeaderboardSortOrder sortOrder = LeaderboardSortOrder.Mmr)
        {
            if (!_seasonStats.TryGetValue((game, season), out var seasonStats))
                return Array.Empty<PlayerLeaderboardData>();

            if (seasonStats.PlayerSortOrders.TryGetValue(sortOrder, out var sortedStats))
                return sortedStats;

            sortedStats = GetSortedPlayerData(seasonStats.Players.Values, sortOrder);
            seasonStats.PlayerSortOrders[sortOrder] = sortedStats;
            return sortedStats;
        }

        public IReadOnlySet<string> GetAllCountryCodes(GameMode game, int season)
        {
            return _seasonStats.TryGetValue((game, season), out var seasonData) ? seasonData.CountryCodes : ImmutableHashSet<string>.Empty;
        }

        public bool TryGetPlayerStatsById(int id, GameMode game, int season, [NotNullWhen(true)] out PlayerLeaderboardData? playerStat)
        {
            playerStat = null;
            return _seasonStats.TryGetValue((game, season), out var seasonStats) && seasonStats.Players.TryGetValue(id, out playerStat);
        }

        public void OnChange(IDbCache dbCache)
        {
            var newSeasonStats = new Dictionary<(GameMode Game, int Season), SeasonStatsData>();
            var countryNames = _loungeSettingsService.CountryNames;
            foreach (var game in _loungeSettingsService.ValidGames)
            {
                var seasons = _loungeSettingsService.ValidSeasons[game];
                var registrations = dbCache.PlayerGameRegistrations[game.GetRegistrationGameMode()];

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
                    foreach (var table in dbCache.Tables.Values
                        .Where(v => v.Season == season && v.Game == game && v.VerifiedOn != null && v.DeletedOn == null))
                    {
                        var tableScores = dbCache.TableScores.GetValueOrDefault(table.Id)?.Values?.ToList();
                        if (tableScores is null)
                            continue;

                        var numPlayers = tableScores.Count;
                        var eventData = new EventData(table.Id, table.NumTeams, numPlayers, table.Tier, table.VerifiedOn!.Value);
                        tablesLookup[table.Id] = eventData;

                        var formatMultiplier = string.Equals(table.Tier, "SQ", StringComparison.OrdinalIgnoreCase) ? sqMultiplier : 1;

                        foreach (var tableScore in tableScores)
                        {
                            var multiplier = tableScore.Multiplier * formatMultiplier;
                            var mmrDelta = tableScore.NewMmr!.Value - tableScore.PrevMmr!.Value;
                            var partnerScores = tableScores.Where(s => s.Team == tableScore.Team && s.PlayerId != tableScore.PlayerId).Select(s => s.Score).ToList();
                            var playerEventData = new PlayerEventData(tableScore.TableId, tableScore.Score, tableScore.Multiplier, mmrDelta, partnerScores, eventData);
                            if (!playerEventsLookup.ContainsKey(tableScore.PlayerId))
                            {
                                Console.Error.WriteLine($"Player game registration for player {tableScore.PlayerId} in game {game} not found when processing table {table.Id}.");
                                continue;
                            }
                            playerEventsLookup[tableScore.PlayerId].Add(playerEventData);
                        }
                    }

                    // sort all the event lists and update all the player lookups
                    foreach (var playerId in playerLookup.Keys)
                    {
                        var sortedEventsList = playerEventsLookup[playerId].OrderByDescending(e => tablesLookup[e.TableId].VerifiedOn).ToList();
                        var updated = playerLookup[playerId].WithUpdatedEvents(sortedEventsList);

                        // Compute earliest registration date across all games for this player
                        DateTime? minRegisteredOn = null;
                        foreach (var gameRegistrations in dbCache.PlayerGameRegistrations.Values)
                        {
                            if (gameRegistrations.TryGetValue(playerId, out var reg))
                            {
                                if (minRegisteredOn is null || reg.RegisteredOn < minRegisteredOn)
                                    minRegisteredOn = reg.RegisteredOn;
                            }
                        }

                        updated = updated with { AccountCreationDateUtc = minRegisteredOn };
                        playerLookup[playerId] = updated;
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

                    // Calculate last week rank changes
                    var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
                    var playersWithLastWeekRanks = CalculateLastWeekRankChanges(playerLookup, tablesLookup, playerEventsLookup, oneWeekAgo);
                    
                    newSeasonStats[key] = new SeasonStatsData(countryCodes, playersWithLastWeekRanks, new Dictionary<LeaderboardSortOrder, IReadOnlyList<PlayerLeaderboardData>>());
                }
            }

            _seasonStats = newSeasonStats;
        }

        private static IReadOnlyList<PlayerLeaderboardData> GetSortedPlayerData(IEnumerable<PlayerLeaderboardData> playerData, LeaderboardSortOrder sortOrder)
        {
            // filter out hidden players
            playerData = playerData.Where(p => !p.IsHidden);

            playerData = sortOrder switch
            {
                LeaderboardSortOrder.Name => playerData.OrderBy(p => p.Name),
                LeaderboardSortOrder.Mmr => playerData.OrderBy(p => p.OverallRank).ThenBy(p => p.Name),
                LeaderboardSortOrder.MaxMmr => playerData.OrderByDescending(p => p.MaxMmr).ThenBy(p => p.OverallRank).ThenBy(s => s.Name),
                LeaderboardSortOrder.EventsPlayed => playerData
                    .OrderByDescending(s => s.EventsPlayed)
                    .ThenBy(s => s.OverallRank)
                    .ThenBy(s => s.Name),
                LeaderboardSortOrder.LastWeekRankChange => playerData
                    .OrderByDescending(s => s.LastWeekRankChange.HasValue)
                    .ThenBy(s => s.LastWeekRankChange) // Lower rank change is better (negative = rank improved)
                    .ThenBy(s => s.OverallRank)
                    .ThenBy(s => s.Name),
                LeaderboardSortOrder.AvgScore12P => playerData
                    .OrderByDescending(s => s.AverageScore12P.HasValue)
                    .ThenByDescending(s => s.AverageScore12P)
                    .ThenBy(s => s.OverallRank)
                    .ThenBy(s => s.Name),
                LeaderboardSortOrder.AvgScore24P => playerData
                    .OrderByDescending(s => s.AverageScore24P.HasValue)
                    .ThenByDescending(s => s.AverageScore24P)
                    .ThenBy(s => s.OverallRank)
                    .ThenBy(s => s.Name),
                _ => playerData.OrderBy(p => p.OverallRank).ThenBy(p => p.Name)
            };

            return playerData.ToList();
        }

        private static Dictionary<int, PlayerLeaderboardData> CalculateLastWeekRankChanges(
            Dictionary<int, PlayerLeaderboardData> currentPlayerLookup,
            Dictionary<int, EventData> tablesLookup,
            Dictionary<int, List<PlayerEventData>> playerEventsLookup,
            DateTime oneWeekAgo)
        {
            // Step 1: Calculate MMR state one week ago for each player
            var playersOneWeekAgo = new Dictionary<int, (int? Mmr, bool HasEvents, bool IsHidden)>();
            
            foreach (var (playerId, playerData) in currentPlayerLookup)
            {
                // Start with current MMR and work backwards
                int? mmrOneWeekAgo = playerData.Mmr;
                bool hadEventsOneWeekAgo = false;
                
                // Subtract MMR deltas from events that happened in the last week
                var eventsInLastWeek = playerEventsLookup[playerId]
                    .Where(e => tablesLookup[e.TableId].VerifiedOn >= oneWeekAgo)
                    .OrderBy(e => tablesLookup[e.TableId].VerifiedOn);
                
                foreach (var eventData in eventsInLastWeek)
                {
                    if (mmrOneWeekAgo.HasValue)
                    {
                        mmrOneWeekAgo -= eventData.MmrDelta;
                    }
                }
                
                // Check if player had any events before one week ago
                hadEventsOneWeekAgo = playerEventsLookup[playerId]
                    .Any(e => tablesLookup[e.TableId].VerifiedOn < oneWeekAgo);
                
                // If they had no events before one week ago, they shouldn't be ranked
                if (!hadEventsOneWeekAgo)
                {
                    mmrOneWeekAgo = null;
                }
                
                playersOneWeekAgo[playerId] = (mmrOneWeekAgo, hadEventsOneWeekAgo || eventsInLastWeek.Any(), playerData.IsHidden);
            }
            
            // Step 2: Calculate ranks one week ago (only for players who had events and aren't hidden)
            var rankedPlayersOneWeekAgo = playersOneWeekAgo
                .Where(kvp => kvp.Value.HasEvents && !kvp.Value.IsHidden && kvp.Value.Mmr.HasValue)
                .OrderByDescending(kvp => kvp.Value.Mmr)
                .ThenBy(kvp => currentPlayerLookup[kvp.Key].Name)
                .ToList();
            
            var ranksOneWeekAgo = new Dictionary<int, int>();
            int prevRankOneWeekAgo = -1;
            int? prevMmrOneWeekAgo = -1;
            int rankOneWeekAgo = 1;
            
            foreach (var (playerId, (mmr, _, _)) in rankedPlayersOneWeekAgo)
            {
                int actualRankOneWeekAgo = mmr == prevMmrOneWeekAgo ? prevRankOneWeekAgo : rankOneWeekAgo;
                ranksOneWeekAgo[playerId] = actualRankOneWeekAgo;
                
                prevRankOneWeekAgo = actualRankOneWeekAgo;
                prevMmrOneWeekAgo = mmr;
                rankOneWeekAgo++;
            }
            
            // Step 3: Calculate current ranks for only those players who were ranked a week ago
            var eligiblePlayerIds = ranksOneWeekAgo.Keys.ToHashSet();
            var currentEligiblePlayers = currentPlayerLookup.Values
                .Where(p => eligiblePlayerIds.Contains(p.Id) && !p.IsHidden && p.HasEvents)
                .OrderByDescending(p => p.Mmr)
                .ThenBy(p => p.Name)
                .ToList();
            
            var currentRanksForEligible = new Dictionary<int, int>();
            int prevCurrentRank = -1;
            int? prevCurrentMmr = -1;
            int currentRank = 1;
            
            foreach (var playerData in currentEligiblePlayers)
            {
                int actualCurrentRank = playerData.Mmr == prevCurrentMmr ? prevCurrentRank : currentRank;
                currentRanksForEligible[playerData.Id] = actualCurrentRank;
                
                prevCurrentRank = actualCurrentRank;
                prevCurrentMmr = playerData.Mmr;
                currentRank++;
            }
            
            // Step 4: Calculate rank changes and update player data
            var updatedPlayerLookup = new Dictionary<int, PlayerLeaderboardData>();
            
            foreach (var (playerId, playerData) in currentPlayerLookup)
            {
                int? rankChange = null;
                
                if (ranksOneWeekAgo.TryGetValue(playerId, out int oldRank) && 
                    currentRanksForEligible.TryGetValue(playerId, out int newRank))
                {
                    rankChange = newRank - oldRank; // Positive = rank dropped, Negative = rank improved
                }
                
                updatedPlayerLookup[playerId] = playerData with { LastWeekRankChange = rankChange };
            }
            
            return updatedPlayerLookup;
        }
    }
}
