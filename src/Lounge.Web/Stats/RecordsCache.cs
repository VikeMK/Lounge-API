using Lounge.Web.Data.ChangeTracking;
using Lounge.Web.Data.Entities;
using Lounge.Web.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lounge.Web.Stats
{
    public class RecordsCache : IDbCacheUpdateSubscriber, IRecordsCache
    {
        public record Player(int Id, string Name, string? CountryCode);
        public record Record(int TableId, IReadOnlyList<Player> Players, int TotalScore);
        public record FormatRecord(IReadOnlyList<Record> Results);
        public record TierRecords(IReadOnlyDictionary<int, FormatRecord> TeamCounts);
        public record SeasonRecords(IReadOnlyDictionary<string, TierRecords> Tiers);

        private IReadOnlyDictionary<(Game Game, int Season), SeasonRecords> _resultsBySeason = new Dictionary<(Game Game, int Season), SeasonRecords>();

        public SeasonRecords GetRecords(Game game, int season)
        {
            if (_resultsBySeason.TryGetValue((game, season), out SeasonRecords? results))
                return results;

            return new SeasonRecords(new Dictionary<string, TierRecords>(StringComparer.OrdinalIgnoreCase));
        }

        public void OnChange(IDbCache dbCache)
        {
            _resultsBySeason = dbCache.Tables.Values
                .Where(t => t.VerifiedOn != null && t.DeletedOn == null)
                .GroupBy(s => ((Game)s.Game, s.Season))
                .ToDictionary(s => s.Key, tables => GetSeasonRecords(tables, dbCache));
        }

        private static SeasonRecords GetSeasonRecords(IEnumerable<Table> seasonTables, IDbCache dbCache)
        {
            var dict = seasonTables
                .GroupBy(t => t.Tier)
                .Where(t => t.Key != "SQ")
                .ToDictionary(
                    s => s.Key,
                    s => GetTierRecords(s, dbCache),
                    StringComparer.OrdinalIgnoreCase);

            return new(dict);
        }

        private static TierRecords GetTierRecords(IEnumerable<Table> seasonTables, IDbCache dbCache)
        {
            var dict = seasonTables
                .Where(t => dbCache.TableScores[t.Id].Count == 12) // TODO: Add support for 24 player records
                .GroupBy(t => t.NumTeams)
                .ToDictionary(
                    s => s.Key,
                    s => GetFormatRecords(s, dbCache));

            return new(dict);
        }

        private static FormatRecord GetFormatRecords(IEnumerable<Table> tierTables, IDbCache dbCache)
        {
            var records = tierTables
                .SelectMany(t =>
                    dbCache.TableScores[t.Id].Values
                        .GroupBy(s => s.Team)
                        .Select(s => (TableId: t.Id, Team: s.Key, TotalScore: s.Sum(r => r.Score))))
                .OrderByDescending(t => t.TotalScore)
                .Take(10)
                .Select(record =>
                {
                    var players = dbCache.TableScores[record.TableId].Values
                        .Where(t => t.Team == record.Team)
                        .Select(s =>
                        {
                            var player = dbCache.Players[s.PlayerId];
                            return new Player(player.Id, player.Name, player.CountryCode);
                        })
                        .ToList();

                    return new Record(record.TableId, players, record.TotalScore);
                })
                .ToList();

            return new(records);
        }
    }
}
