using Lounge.Web.Data;
using Lounge.Web.Models;
using Lounge.Web.Models.ViewModels;
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

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Leaderboard));
        }

        [Route("Leaderboard")]
        public async Task<IActionResult> Leaderboard(int page = 1, string? filter = null)
        {
            if (page <= 0)
            {
                return BadRequest("Page must be a number 1 or greater");
            }

            var normalizedFilter = filter == null ? null : PlayerUtils.NormalizeName(filter);

            var playerEntities = await _context.PlayerStats
                .AsNoTracking()
                .Where(s => filter == null || s.NormalizedName.Contains(normalizedFilter))
                .OrderBy(p => p.Rank)
                .Skip(PageSize * (page - 1))
                .Take(PageSize)
                .ToListAsync();

            var playerCount = await _context.Players.CountAsync();
            var maxPageNum = (int)Math.Ceiling(playerCount / (decimal)PageSize);
            page = Math.Clamp(page, 1, maxPageNum);

            var playerViewModels = new List<LeaderboardViewModel.Player>();
            foreach (var p in playerEntities)
            {
                decimal? winRate = p.EventsPlayed == 0 ? null : p.Wins / p.EventsPlayed;

                playerViewModels.Add(new LeaderboardViewModel.Player
                {
                    Id = p.Id,
                    OverallRank = p.EventsPlayed == 0 ? null : p.Rank,
                    Name = p.Name,
                    Mmr = p.Mmr,
                    MaxMmr = p.MaxMmr,
                    EventsPlayed = p.EventsPlayed,
                    WinRate = winRate,
                    WinsLastTen = p.LastTenWins,
                    LossesLastTen = p.LastTenLosses,
                    GainLossLastTen = p.LastTenGainLoss,
                    LargestGain = p.LargestGain < 0 ? null : p.LargestGain,
                    LargestLoss = p.LargestLoss > 0 ? null : p.LargestLoss,
                    MmrRank = RankUtils.GetRank(p.Mmr),
                    MaxMmrRank = RankUtils.GetRank(p.MaxMmr)
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

        [Route("PlayerDetails/{id}")]
        public async Task<IActionResult> PlayerDetails(int id)
        {
            var player = await _context.Players
                .AsNoTracking()
                .SelectPropertiesForPlayerDetails()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (player is null)
                return NotFound();

            return View(PlayerUtils.GetPlayerDetails(player));
        }

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
    }
}
