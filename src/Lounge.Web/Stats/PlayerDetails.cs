using System.Collections.Generic;

namespace Lounge.Web.Stats
{
    public record PlayerDetails(
        int Id,
        IReadOnlyList<int> PlacementIds,
        IReadOnlyList<int> TableIds,
        IReadOnlyList<int> PenaltyIds,
        IReadOnlyList<int> BonusIds,
        IReadOnlyList<int> NameChangeIds);
}
