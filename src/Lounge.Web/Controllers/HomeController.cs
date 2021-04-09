using Lounge.Web.Data;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Stats;
using Lounge.Web.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lounge.Web.Controllers
{
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : Controller
    {
        private const int PageSize = 100;

        private readonly ApplicationDbContext _context;
        private readonly IPlayerStatCache _playerStatCache;
        private readonly IPlayerStatService _playerStatService;

        public HomeController(
            ApplicationDbContext context,
            IPlayerStatCache playerStatCache,
            IPlayerStatService playerStatService)
        {
            _context = context;
            _playerStatCache = playerStatCache;
            _playerStatService = playerStatService;
        }

        [ResponseCache(Duration = 180)]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Leaderboard));
        }

        [ResponseCache(Duration = 180, VaryByQueryKeys = new string[] { "*" })]
        [Route("Leaderboard")]
        public async Task<IActionResult> Leaderboard(int page = 1, string? filter = null)
        {
            if (page <= 0)
            {
                return BadRequest("Page must be a number 1 or greater");
            }

            int? playerId = null;
            if (filter != null && filter.StartsWith("mkc="))
            {
                if (int.TryParse(filter[4..], out int mkcId))
                {
                    playerId = await _context.Players
                        .Where(p => p.MKCId == mkcId)
                        .Select(p => p.Id)
                        .FirstOrDefaultAsync();
                }

                // if no player exists with that MKC ID then we should show nothing so set player ID to -1
                playerId ??= -1;
            }

            IReadOnlyList<RankedPlayerStat> playerEntities;
            int totalPlayerCount;
            if (playerId is int id)
            {
                RankedPlayerStat? playerStat = await GetPlayerStatsAsync(id);
                playerEntities = playerStat == null ? Array.Empty<RankedPlayerStat>() : new[] { playerStat };
                totalPlayerCount = playerEntities.Count;
            }
            else
            {
                var leaderboard = _playerStatCache.GetAllStats();
                if (filter != null)
                {
                    var normalized = PlayerUtils.NormalizeName(filter);
                    leaderboard = leaderboard.Where(s => s.Stat.NormalizedName.Contains(filter)).ToList();
                }

                playerEntities = leaderboard
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
                totalPlayerCount = leaderboard.Count;
            }

            // clever trick to get number of pages https://stackoverflow.com/a/503201
            var maxPageNum = (totalPlayerCount - 1) / PageSize + 1;
            page = Math.Clamp(page, 1, maxPageNum);

            var playerViewModels = new List<LeaderboardViewModel.Player>();
            foreach (var rankedPlayerStat in playerEntities)
            {
                var playerStat = rankedPlayerStat.Stat;
                decimal? winRate = playerStat.EventsPlayed == 0 ? null : (decimal)playerStat.Wins / playerStat.EventsPlayed;

                playerViewModels.Add(new LeaderboardViewModel.Player
                {
                    Id = playerStat.Id,
                    OverallRank = playerStat.EventsPlayed == 0 ? null : rankedPlayerStat.Rank,
                    Name = playerStat.Name,
                    Mmr = playerStat.Mmr,
                    MaxMmr = playerStat.MaxMmr,
                    EventsPlayed = playerStat.EventsPlayed,
                    WinRate = winRate,
                    WinsLastTen = playerStat.LastTenWins,
                    LossesLastTen = playerStat.LastTenLosses,
                    GainLossLastTen = playerStat.LastTenGainLoss,
                    LargestGain = playerStat.LargestGain < 0 ? null : playerStat.LargestGain,
                    LargestLoss = playerStat.LargestLoss > 0 ? null : playerStat.LargestLoss,
                    MmrRank = RankUtils.GetRank(playerStat.Mmr),
                    MaxMmrRank = RankUtils.GetRank(playerStat.MaxMmr)
                });
            }

            return View(new LeaderboardViewModel
            {
                Players = playerViewModels,
                Page = page,
                HasNextPage = page < maxPageNum,
                HasPrevPage = page > 1,
                Filter = filter
            });
        }

        [ResponseCache(Duration = 180, VaryByQueryKeys = new string[] { "*" })]
        [Route("PlayerDetails/{id}")]
        public async Task<IActionResult> PlayerDetails(int id)
        {
            var player = await _context.Players
                .AsNoTracking()
                .SelectPropertiesForPlayerDetails()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (player is null)
                return NotFound();

            var playerStat = await GetPlayerStatsAsync(id);
            if (playerStat is null)
                return NotFound();

            return View(PlayerUtils.GetPlayerDetails(player, playerStat));
        }

        [ResponseCache(Duration = 180, VaryByQueryKeys = new string[] { "*" })]
        [Route("TableDetails/{id}")]
        public async Task<IActionResult> TableDetails(int id)
        {
            var table = await _context.Tables
                .AsNoTracking()
                .SelectPropertiesForTableDetails()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (table is null)
                return NotFound();

            return View(TableUtils.GetTableDetails(table));
        }

        // since table images are expensive, lets let them be cached for 30 minutes
        [ResponseCache(Duration = 30 * 60, VaryByQueryKeys = new string[] { "*" })]
        [Route("TableImage/{id}.png")]
        public async Task<IActionResult> TableImage(int id)
        {
            var table = await _context.Tables.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

            if (table is null)
                return NotFound();

            if (table.TableImageData is null)
            {
                table.TableImageData = await TableUtils.GetImageAsBase64UrlAsync(table.Url);
                _ = await _context.SaveChangesAsync();
            }

            var bytes = Convert.FromBase64String(table.TableImageData);
            return File(bytes, "image/png");
        }

        [Route("/error")]
        public IActionResult Error() => Problem();

        private async Task<RankedPlayerStat?> GetPlayerStatsAsync(int id)
        {
            RankedPlayerStat? playerStat = null;
            if (id != -1)
            {
                if (!_playerStatCache.TryGetPlayerStatsById(id, out playerStat))
                {
                    var stat = await _playerStatService.GetPlayerStatsByIdAsync(id);
                    if (stat is not null)
                    {
                        _playerStatCache.UpdatePlayerStats(stat);
                        _playerStatCache.TryGetPlayerStatsById(id, out playerStat);
                    }
                }
            }

            return playerStat;
        }
    }
}
