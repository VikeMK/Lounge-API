using Lounge.Web.Data.Entities;
using System.Collections.Generic;

namespace Lounge.Web.Data.ChangeTracking
{
    public class DbCacheData : IDbCache
    {
        public required IReadOnlyDictionary<int, Bonus> Bonuses { get; set; }
        public required IReadOnlyDictionary<int, Penalty> Penalties { get; set; }
        public required IReadOnlyDictionary<int, Placement> Placements { get; set; }
        public required IReadOnlyDictionary<int, Player> Players { get; set; }
        public required IReadOnlyDictionary<int, Dictionary<int, PlayerSeasonData>> PlayerSeasonData { get; set; }
        public required IReadOnlyDictionary<int, Table> Tables { get; set; }
        public required IReadOnlyDictionary<int, Dictionary<int, TableScore>> TableScores { get; set; }
        public required IReadOnlyDictionary<int, NameChange> NameChanges { get; set; }
    }
}