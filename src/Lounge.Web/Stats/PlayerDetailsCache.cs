using Lounge.Web.Data.ChangeTracking;
using Lounge.Web.Settings;
using Lounge.Web.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Lounge.Web.Stats
{
    public class PlayerDetailsCache : IPlayerDetailsCache, IDbCacheUpdateSubscriber
    {
        private readonly ILoungeSettingsService _loungeSettingsService;
        private IReadOnlyDictionary<string, int> _playerIdByNameLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private IReadOnlyDictionary<string, int> _playerIdByDiscordLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private IReadOnlyDictionary<string, int> _playerIdByFCLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private IReadOnlyDictionary<int, IReadOnlyDictionary<int, PlayerDetails>> _playerDetailsLookupBySeason = new Dictionary<int, IReadOnlyDictionary<int, PlayerDetails>>();

        public PlayerDetailsCache(ILoungeSettingsService loungeSettingsService)
        {
            _loungeSettingsService = loungeSettingsService;
        }

        public bool TryGetPlayerDetailsById(int playerId, int season, [NotNullWhen(returnValue: true)] out PlayerDetails? playerDetails)
        {
            if (_playerDetailsLookupBySeason.TryGetValue(season, out var playerDetailsLookup))
            {
                return playerDetailsLookup.TryGetValue(playerId, out playerDetails);
            }

            playerDetails = null;
            return false;
        }

        public bool TryGetPlayerIdByName(string name, [NotNullWhen(returnValue: true)] out int? playerId)
        {
            var normalizedName = PlayerUtils.NormalizeName(name);
            var hasValue = _playerIdByNameLookup.TryGetValue(normalizedName, out var id);
            playerId = id;
            return hasValue;
        }

        public bool TryGetPlayerIdByDiscord(string discord, [NotNullWhen(true)] out int? playerId)
        {
            var hasValue = _playerIdByDiscordLookup.TryGetValue(discord, out var id);
            playerId = id;
            return hasValue;
        }

        public bool TryGetPlayerIdByFC(string fc, [NotNullWhen(true)] out int? playerId)
        {
            var hasValue = _playerIdByFCLookup.TryGetValue(fc, out var id);
            playerId = id;
            return hasValue;
        }

        public void OnChange(IDbCache dbCache)
        {
            var newPlayerDetailsLookup = new Dictionary<int, IReadOnlyDictionary<int, PlayerDetails>>();
            var seasons = _loungeSettingsService.ValidSeasons;

            var nameChangesByPlayer = dbCache.NameChanges.Values
                .GroupBy(p => p.PlayerId)
                .ToDictionary(p => p.Key, p => p.Select(x => x.Id).ToList());

            foreach (var season in seasons)
            {
                var placementsByPlayer = dbCache.Placements.Values
                    .Where(p => p.Season == season)
                    .GroupBy(p => p.PlayerId)
                    .ToDictionary(p => p.Key, p => p.Select(x => x.Id).ToList());

                var tableScoresByPlayer = dbCache.TableScores.Values
                    .SelectMany(s => s.Values)
                    .Where(p => dbCache.Tables.TryGetValue(p.TableId, out var table) && table.Season == season)
                    .GroupBy(p => p.PlayerId)
                    .ToDictionary(p => p.Key, p => p.Select(x => x.TableId).ToList());

                var penaltiesByPlayer = dbCache.Penalties.Values
                    .Where(p => p.Season == season)
                    .GroupBy(p => p.PlayerId)
                    .ToDictionary(p => p.Key, p => p.Select(x => x.Id).ToList());

                var bonusesByPlayer = dbCache.Bonuses.Values
                    .Where(p => p.Season == season)
                    .GroupBy(p => p.PlayerId)
                    .ToDictionary(p => p.Key, p => p.Select(x => x.Id).ToList());

                newPlayerDetailsLookup[season] = dbCache.Players.Values
                    .ToDictionary(
                        p => p.Id,
                        p => new PlayerDetails(
                            p.Id,
                            placementsByPlayer.GetValueOrDefault(p.Id) ?? new List<int>(),
                            tableScoresByPlayer.GetValueOrDefault(p.Id) ?? new List<int>(),
                            penaltiesByPlayer.GetValueOrDefault(p.Id) ?? new List<int>(),
                            bonusesByPlayer.GetValueOrDefault(p.Id) ?? new List<int>(),
                            nameChangesByPlayer.GetValueOrDefault(p.Id) ?? new List<int>()));
            }

            var playerNamesToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var playerDiscordsToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var playerFCsToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach ((var playerId, var player) in dbCache.Players)
            {
                playerNamesToId[player.NormalizedName] = playerId;

                if (player.DiscordId is string discordId)
                    playerDiscordsToId[discordId] = playerId;

                if (player.SwitchFc is string switchFC)
                    playerFCsToId[switchFC] = playerId;
            }

            _playerIdByNameLookup = playerNamesToId;
            _playerIdByDiscordLookup = playerDiscordsToId;
            _playerIdByFCLookup = playerFCsToId;
            _playerDetailsLookupBySeason = newPlayerDetailsLookup;
        }
    }
}
