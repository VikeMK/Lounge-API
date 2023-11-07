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
        bool HasEvents,
        int EventsPlayed,
        decimal? WinRate,
        int LastTenWins,
        int LastTenLosses,
        int LastTenGainLoss,
        (int Amount, int EventId)? LargestGain,
        (int Amount, int EventId)? LargestLoss,
        double? AverageScore,
        double? NoSQAverageScore,
        double? AverageLastTen,
        double? NoSQAverageLastTen,
        double? PartnerAverage,
        double? NoSQPartnerAverage,
        int? OverallRank = null)
    {
        public PlayerLeaderboardData(
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
            int? OverallRank = null) : this(Id, Name, MkcId, DiscordId, RegistryId, CountryCode, SwitchFc, IsHidden, Mmr, MaxMmr,
                HasEvents: Events.Any(),
                EventsPlayed: Events.Count,
                WinRate: Events.Any() ? (decimal)Events.Count(e => e.IsWin) / Events.Count : null,
                LastTenWins: Events.Take(10).Count(e => e.IsWin),
                LastTenLosses: Events.Take(10).Count(e => !e.IsWin),
                LastTenGainLoss: Events.Take(10).Sum(e => e.MmrDelta),
                LargestGain: Events.Where(e => e.MmrDelta > 0).Max(e => ((int, int)?)(e.MmrDelta, e.TableId)),
                LargestLoss: Events.Where(e => e.MmrDelta < 0).Min(e => ((int, int)?)(e.MmrDelta, e.TableId)),
                AverageScore: Events.Average(e => (int?)e.Score),
                NoSQAverageScore: Events.Where(e => e.Event.Tier != "SQ").Average(e => (int?)e.Score),
                AverageLastTen: Events.Take(10).Average(e => (int?)e.Score),
                NoSQAverageLastTen: Events.Where(e => e.Event.Tier != "SQ").Take(10).Average(e => (int?)e.Score),
                PartnerAverage: Events.SelectMany(p => p.PartnerScores).Cast<int?>().Average(),
                NoSQPartnerAverage: Events.Where(e => e.Event.Tier != "SQ").SelectMany(p => p.PartnerScores).Cast<int?>().Average(),
                OverallRank)
        {
        }

        public PlayerLeaderboardData WithUpdatedEvents(IReadOnlyList<PlayerEventData> Events)
        {
            return new PlayerLeaderboardData(Id, Name, MkcId, DiscordId, RegistryId, CountryCode, SwitchFc, IsHidden, Mmr, MaxMmr, Events, OverallRank);
        }
    }
}
