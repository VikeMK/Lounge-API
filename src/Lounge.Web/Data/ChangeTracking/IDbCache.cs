using Lounge.Web.Data.Entities;
using System.Collections.Generic;

namespace Lounge.Web.Data.ChangeTracking
{
    public interface IDbCache
    {
        IReadOnlyDictionary<int, Bonus> Bonuses { get; }
        IReadOnlyDictionary<int, Penalty> Penalties { get; }
        IReadOnlyDictionary<int, Placement> Placements { get; }
        IReadOnlyDictionary<int, Player> Players { get; }
        IReadOnlyDictionary<int, Dictionary<int, PlayerSeasonData>> PlayerSeasonData { get; }
        IReadOnlyDictionary<int, Table> Tables { get; }
        IReadOnlyDictionary<int, Dictionary<int, TableScore>> TableScores { get; }
        IReadOnlyDictionary<int, NameChange> NameChanges { get; }
    }
}