using Lounge.Web.Models;
using System.Collections.Generic;

namespace Lounge.Web.Data.ChangeTracking
{
    public interface IChangeTrackingSubscriber
    {
        void HandleChanges(List<BonusChange> bonuses, List<PenaltyChange> penalties, List<PlacementChange> placements, List<PlayerChange> players, List<PlayerSeasonDataChange> playerSeasonData, List<TableChange> tables, List<TableScoreChange> tableScores);
        void Initialize(List<Bonus> bonuses, List<Penalty> penalties, List<Placement> placements, List<Player> players, List<PlayerSeasonData> playerSeasonData, List<Table> tables, List<TableScore> tableScores);
    }
}