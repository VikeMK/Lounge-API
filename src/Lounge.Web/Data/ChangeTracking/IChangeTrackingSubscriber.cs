using Lounge.Web.Data.Entities;
using Lounge.Web.Data.Entities.ChangeTracking;
using System.Collections.Generic;

namespace Lounge.Web.Data.ChangeTracking
{
    public interface IChangeTrackingSubscriber
    {
        void HandleChanges(List<BonusChange> bonuses, List<PenaltyChange> penalties, List<PlacementChange> placements, List<PlayerChange> players, List<PlayerSeasonDataChange> playerSeasonData, List<TableChange> tables, List<TableScoreChange> tableScores, List<NameChangeChange> nameChanges);
        void Initialize(IEnumerable<Bonus> bonuses, IEnumerable<Penalty> penalties, IEnumerable<Placement> placements, IEnumerable<Player> players, IEnumerable<PlayerSeasonData> playerSeasonData, IEnumerable<Table> tables, IEnumerable<TableScore> tableScores, IEnumerable<NameChange> nameChanges);
    }
}