using Lounge.Web.Data.Entities;
using Lounge.Web.Models.Enums;
using System.Collections.Generic;

namespace Lounge.Web.Data.ChangeTracking
{
    public class DbCacheData : IDbCache
    {
        public required IReadOnlyDictionary<int, Bonus> Bonuses { get; set; }
        public required IReadOnlyDictionary<int, Penalty> Penalties { get; set; }
        public required IReadOnlyDictionary<int, Placement> Placements { get; set; }
        public required IReadOnlyDictionary<int, Player> Players { get; set; }
        public required IReadOnlyDictionary<RegistrationGameMode, Dictionary<int, PlayerGameRegistration>> PlayerGameRegistrations { get; set; }
        public required IReadOnlyDictionary<(GameMode Game, int Season), Dictionary<int, PlayerSeasonData>> PlayerSeasonData { get; set; }
        public required IReadOnlyDictionary<int, Table> Tables { get; set; }
        public required IReadOnlyDictionary<int, Dictionary<int, TableScore>> TableScores { get; set; }
        public required IReadOnlyDictionary<int, NameChange> NameChanges { get; set; }
    }
}