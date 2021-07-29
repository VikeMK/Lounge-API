using Lounge.Web.Models;
using System.Linq;

namespace Lounge.Web.Stats
{
    public static class PlayerStatsExtensions
    {
        public static IQueryable<PlayerStat> SelectPlayerStats(this IQueryable<Player> player) =>
            player
                .Select(p => new
                {
                    Player = p,
                    AllTime = p.TableScores.Where(s => s.Table.VerifiedOn != null && s.Table.DeletedOn == null),
                    LastTen = p.TableScores
                        .Where(s => s.Table.VerifiedOn != null && s.Table.DeletedOn == null)
                        .OrderByDescending(s => s.Table.VerifiedOn)
                        .Take(10)
                })
                .Select(p => new PlayerStat(p.Player.Id, p.Player.Name, p.Player.NormalizedName)
                {
                    Mmr = p.Player.Mmr,
                    MaxMmr = p.Player.MaxMmr,
                    EventsPlayed = p.AllTime.Count(),
                    Wins = p.AllTime.Count(s => s.NewMmr > s.PrevMmr),
                    LargestGain = p.AllTime.Where(s => s.NewMmr > s.PrevMmr).Max(s => s.NewMmr - s.PrevMmr),
                    LargestLoss = p.AllTime.Where(s => s.NewMmr < s.PrevMmr).Min(s => s.NewMmr - s.PrevMmr),
                    LastTenGainLoss = p.LastTen.Sum(s => s.NewMmr - s.PrevMmr),
                    LastTenWins = p.LastTen.Count(s => s.NewMmr > s.PrevMmr),
                    LastTenLosses = p.LastTen.Count(s => !(s.NewMmr > s.PrevMmr)),
                });
    }
}
