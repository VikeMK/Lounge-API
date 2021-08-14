using Lounge.Web.Models;
using System.Linq;

namespace Lounge.Web.Stats
{
    public static class PlayerStatsExtensions
    {
        public static IQueryable<PlayerStat> SelectPlayerStats(this IQueryable<Player> player, int season) =>
            player
                .Select(p => new
                {
                    Player = p,
                    SeasonData = p.SeasonData.FirstOrDefault(s => s.Season == season),
                    AllTime = p.TableScores.Where(s => s.Table.VerifiedOn != null && s.Table.DeletedOn == null && s.Table.Season == season),
                    LastTen = p.TableScores
                        .Where(s => s.Table.VerifiedOn != null && s.Table.DeletedOn == null && s.Table.Season == season)
                        .OrderByDescending(s => s.Table.VerifiedOn)
                        .Take(10)
                })
                .Select(p => new PlayerStat(p.Player.Id, p.Player.Name, p.Player.NormalizedName)
                {
                    CountryCode = p.Player.CountryCode,
                    IsHidden = p.Player.IsHidden,
                    Mmr = p.SeasonData.Mmr,
                    MaxMmr = p.SeasonData.MaxMmr,
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
