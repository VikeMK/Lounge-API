using Lounge.Web.Data;
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
        public async Task<IActionResult> Leaderboard(int page = 1)
        {
            if (page <= 0)
            {
                return BadRequest("Page must be a number 1 or greater");
            }

            var playerEntities = await _context.Players
                .AsNoTracking()
                .OrderByDescending(p => p.Mmr)
                .Skip(PageSize * (page - 1))
                .Take(PageSize)
                .Select(p => new
                {
                    p.Mmr,
                    p.Id,
                    p.Name,
                    p.MaxMmr,
                    EventsPlayed = p.TableScores.Count(t => t.NewMmr != null),
                    Wins = p.TableScores.Count(t => t.NewMmr != null && t.NewMmr > t.PrevMmr),
                    LastTen = p.TableScores
                        .Where(t => t.Table.VerifiedOn != null)
                        .OrderByDescending(t => t.Table.VerifiedOn)
                        .Take(10)
                        .Select(t => t.NewMmr - t.PrevMmr)
                        .ToList(),
                    LargestGain = p.TableScores.Where(t => t.NewMmr != null).Max(t => t.NewMmr - t.PrevMmr),
                    LargestLoss = p.TableScores.Where(t => t.NewMmr != null).Min(t => t.NewMmr - t.PrevMmr),
                })
                .ToListAsync();

            var playerCount = await _context.Players.CountAsync();
            var maxPageNum = (int)Math.Ceiling(playerCount / (decimal)PageSize);
            page = Math.Clamp(page, 1, maxPageNum);

            var playerViewModels = new List<LeaderboardViewModel.Player>();
            int? prevMMR = -1;
            int rank = 1 + PageSize * (page - 1);
            int prevRank = 0;
            foreach (var p in playerEntities)
            {
                var playerRank = prevMMR == p.Mmr ? prevRank : rank;

                decimal? winRate = p.EventsPlayed == 0 ? null : (decimal)p.Wins / p.EventsPlayed;

                playerViewModels.Add(new LeaderboardViewModel.Player
                {
                    Id = p.Id,
                    OverallRank = playerRank,
                    Name = p.Name,
                    Mmr = p.Mmr,
                    MaxMmr = p.MaxMmr,
                    EventsPlayed = p.EventsPlayed,
                    WinRate = winRate,
                    WinsLastTen = p.LastTen.Count(t => t > 0),
                    LossesLastTen = p.LastTen.Count(t => t <= 0),
                    GainLossLastTen = p.LastTen.Sum(t => t ?? 0),
                    LargestGain = p.LargestGain < 0 ? null : p.LargestGain,
                    LargestLoss = p.LargestLoss > 0 ? null : p.LargestLoss,
                    MmrRank = RankUtils.GetRank(p.Mmr),
                    MaxMmrRank = RankUtils.GetRank(p.MaxMmr)
                });

                prevMMR = p.Mmr;
                prevRank = playerRank;
                rank++;
            }

            return View(new LeaderboardViewModel
            {
                Players = playerViewModels,
                Page = page,
                HasNextPage = page < maxPageNum,
                HasPrevPage = page > 1
            });
        }

        [Route("PlayerDetails/{id}")]
        public async Task<IActionResult> PlayerDetails(int id)
        {
            var player = await _context.Players
                .AsNoTracking()
                .Include(p => p.Penalties)
                .Include(p => p.TableScores)
                    .ThenInclude(s => s.Table)
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
                .Include(t => t.Scores)
                .ThenInclude(s => s.Player)
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
