using System.Collections.Generic;

namespace Lounge.Web.Stats
{
    public record PlayerEventData(int TableId, int Score, double Multiplier, int MmrDelta, IReadOnlyList<int> PartnerScores, EventData Event)
    {
        public bool IsWin => MmrDelta > 0;
    }
}
