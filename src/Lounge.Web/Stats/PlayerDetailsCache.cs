using Lounge.Web.Data.ChangeTracking;
using Lounge.Web.Settings;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Lounge.Web.Stats
{
    public class PlayerDetailsCache : IPlayerDetailsCache, IDbCacheUpdateSubscriber
    {
        private readonly ILoungeSettingsService _loungeSettingsService;
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

        public void OnChange(IDbCache dbCache)
        {
            var newPlayerDetailsLookup = new Dictionary<int, IReadOnlyDictionary<int, PlayerDetails>>();
            var seasons = _loungeSettingsService.ValidSeasons;
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
                            bonusesByPlayer.GetValueOrDefault(p.Id) ?? new List<int>()));
            }

            _playerDetailsLookupBySeason = newPlayerDetailsLookup;
        }
    }
}
