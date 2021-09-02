using Lounge.Web.Data.Entities;
using Lounge.Web.Data.Entities.ChangeTracking;
using System.Collections.Generic;
using System.Linq;

namespace Lounge.Web.Data.ChangeTracking
{
    public class DbCache : IChangeTrackingSubscriber, IDbCache
    {
        private readonly IDbCacheUpdateSubscriber[] _subscribers;

        private readonly Dictionary<int, Player> _players = new();
        private readonly Dictionary<int, Dictionary<int, PlayerSeasonData>> _playerSeasonData = new();
        private readonly Dictionary<int, Table> _tables = new();
        private readonly Dictionary<int, Dictionary<int, TableScore>> _tableScores = new();
        private readonly Dictionary<int, Penalty> _penalties = new();
        private readonly Dictionary<int, Bonus> _bonuses = new();
        private readonly Dictionary<int, Placement> _placements = new();

        public DbCache(IEnumerable<IDbCacheUpdateSubscriber> subscribers)
        {
            _subscribers = subscribers.ToArray();
        }

        public IReadOnlyDictionary<int, Player> Players => _players;
        public IReadOnlyDictionary<int, Dictionary<int, PlayerSeasonData>> PlayerSeasonData => _playerSeasonData;
        public IReadOnlyDictionary<int, Table> Tables => _tables;
        public IReadOnlyDictionary<int, Dictionary<int, TableScore>> TableScores => _tableScores;
        public IReadOnlyDictionary<int, Penalty> Penalties => _penalties;
        public IReadOnlyDictionary<int, Bonus> Bonuses => _bonuses;
        public IReadOnlyDictionary<int, Placement> Placements => _placements;

        public void Initialize(List<Bonus> bonuses, List<Penalty> penalties, List<Placement> placements, List<Player> players, List<PlayerSeasonData> playerSeasonData, List<Table> tables, List<TableScore> tableScores)
        {
            foreach (var bonus in bonuses)
                _bonuses[bonus.Id] = bonus;

            foreach (var placement in placements)
                _placements[placement.Id] = placement;

            foreach (var player in players)
                _players[player.Id] = player;

            foreach (var table in tables)
                _tables[table.Id] = table;

            foreach (var tableScore in tableScores)
            {
                if (!_tableScores.TryGetValue(tableScore.TableId, out var tableScoresDict))
                {
                    tableScoresDict = new Dictionary<int, TableScore>();
                    _tableScores[tableScore.TableId] = tableScoresDict;
                }

                tableScoresDict[tableScore.PlayerId] = tableScore;
            }

            foreach (var penalty in penalties)
                _penalties[penalty.Id] = penalty;

            foreach (var playerSeasonDataEntry in playerSeasonData)
            {
                if (!_playerSeasonData.TryGetValue(playerSeasonDataEntry.Season, out var seasonData))
                {
                    seasonData = new Dictionary<int, PlayerSeasonData>();
                    _playerSeasonData[playerSeasonDataEntry.Season] = seasonData;
                }

                seasonData[playerSeasonDataEntry.PlayerId] = playerSeasonDataEntry;
            }

            foreach (var subscriber in _subscribers)
                subscriber.OnChange(this);
        }

        public void HandleChanges(List<BonusChange> bonuses, List<PenaltyChange> penalties, List<PlacementChange> placements, List<PlayerChange> players, List<PlayerSeasonDataChange> playerSeasonData, List<TableChange> tables, List<TableScoreChange> tableScores)
        {
            foreach (var bonus in bonuses)
                _bonuses[bonus.Id] = bonus.Entity!;

            foreach (var placement in placements)
                _placements[placement.Id] = placement.Entity!;

            foreach (var player in players)
                _players[player.Id] = player.Entity!;

            foreach (var table in tables)
                _tables[table.Id] = table.Entity!;

            foreach (var tableScore in tableScores)
            {
                if (!_tableScores.TryGetValue(tableScore.TableId, out var tableScoresDict))
                {
                    _tableScores[tableScore.TableId] = tableScoresDict = new Dictionary<int, TableScore>();
                }

                tableScoresDict[tableScore.PlayerId] = tableScore.Entity!;
            }

            foreach (var penalty in penalties)
                _penalties[penalty.Id] = penalty.Entity!;

            foreach (var playerSeasonDataEntry in playerSeasonData)
            {
                if (!_playerSeasonData.TryGetValue(playerSeasonDataEntry.Season, out var seasonData))
                {
                    _playerSeasonData[playerSeasonDataEntry.Season] = seasonData = new Dictionary<int, PlayerSeasonData>();
                }

                seasonData[playerSeasonDataEntry.PlayerId] = playerSeasonDataEntry.Entity!;
            }

            foreach (var subscriber in _subscribers)
                subscriber.OnChange(this);
        }
    }

    public interface IDbCacheUpdateSubscriber
    {
        public void OnChange(IDbCache dbCache);
    }
}
