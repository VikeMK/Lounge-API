using Lounge.Web.Data;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lounge.Web.Controllers
{
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : Controller
    {
        private const int PageSize = 50;
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
            var playerEntities = await _context.Players
                .OrderByDescending(p => p.Mmr)
                .Skip(PageSize * (page - 1))
                .Take(PageSize)
                .ToListAsync();

            var playerViewModels = playerEntities
                .Select(p => new LeaderboardViewModel.Player(id: p.Id, name: p.Name, mmr: p.Mmr, maxMmr: p.MaxMmr))
                .ToList();

            return View(new LeaderboardViewModel(playerViewModels));
        }

        [Route("PlayerDetails/{id}")]
        public async Task<IActionResult> PlayerDetails(int id)
        {
            var player = await _context.Players
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
            var table = await _context.Tables.FindAsync(id);

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
