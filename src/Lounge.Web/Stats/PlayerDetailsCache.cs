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
                    .Where(p => dbCache.Tables[p.TableId].Season == season)
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

            var playerNamesToId = dbCache.Players.Select(p => new KeyValuePair<string, int>(p.Value.NormalizedName, p.Key));
            _playerIdByNameLookup = new Dictionary<string, int>(playerNamesToId, StringComparer.OrdinalIgnoreCase);
            _playerDetailsLookupBySeason = newPlayerDetailsLookup;
        }
    }
}
