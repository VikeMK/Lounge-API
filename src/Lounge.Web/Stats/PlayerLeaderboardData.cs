using System.Collections.Generic;
using System.Linq;

namespace Lounge.Web.Stats
{
    public record PlayerLeaderboardData(
        int Id,
        string Name,
        int MkcId,
        string? DiscordId,
        int? RegistryId,
        string? CountryCode,
        string? SwitchFc,
        bool IsHidden,
        int? Mmr,
        int? MaxMmr,
        IReadOnlyList<PlayerEventData> Events,
        int? OverallRank = null)
    {
        public bool HasEvents => Events.Any();
        public int EventsPlayed => Events.Count;
        public decimal? WinRate => HasEvents ? (decimal)Events.Count(e => e.IsWin) / EventsPlayed : null;
        public int LastTenWins => Events.Take(10).Count(e => e.IsWin);
        public int LastTenLosses => Events.Take(10).Count(e => !e.IsWin);
        public int LastTenGainLoss => Events.Take(10).Sum(e => e.MmrDelta);
        public (int Amount, int EventId)? LargestGain => Events.Where(e => e.MmrDelta > 0).Max(e => ((int, int)?)(e.MmrDelta, e.TableId));
        public (int Amount, int EventId)? LargestLoss => Events.Where(e => e.MmrDelta < 0).Min(e => ((int, int)?)(e.MmrDelta, e.TableId));
    }
}
