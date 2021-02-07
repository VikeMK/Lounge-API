using Lounge.Web.Data;
using Lounge.Web.Models;
using Lounge.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Lounge.Web.Controllers
{
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : Controller
    {
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
        public async Task<IActionResult> Leaderboard()
        {
            var playerEntities = await _context.Players.OrderByDescending(p => p.Mmr).ToListAsync();
            var playerViewModels = playerEntities.Select(p => new LeaderboardViewModel.Player
            {
                Id = p.Id,
                Name = p.Name,
                Mmr = p.Mmr,
                MaxMmr = p.MaxMmr,
            }).ToList();

            return View(new LeaderboardViewModel { Players = playerViewModels });
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

            var mmrChanges = new List<PlayerDetailsViewModel.MmrChange>();
            if (player.InitialMmr is not null)
            {
                mmrChanges.Add(new PlayerDetailsViewModel.MmrChange()
                {
                    ChangeId = null,
                    NewMMR = player.InitialMmr.Value,
                    MmrDelta = 0,
                    Reason = PlayerDetailsViewModel.MmrChangeReason.Placement,
                    Time = player.PlacedOn!.Value
                });
            }

            foreach (var tableScore in player.TableScores)
            {
                if (tableScore.Table.VerifiedOn is null)
                    continue;

                var newMmr = tableScore.NewMmr!.Value;
                var delta = newMmr - tableScore.PrevMmr!.Value;

                mmrChanges.Add(new PlayerDetailsViewModel.MmrChange()
                {
                    ChangeId = tableScore.TableId,
                    NewMMR = newMmr,
                    MmrDelta = delta,
                    Reason = PlayerDetailsViewModel.MmrChangeReason.Table,
                    Time = tableScore.Table.VerifiedOn!.Value,
                });

                if (tableScore.Table.DeletedOn is not null)
                {
                    mmrChanges.Add(new PlayerDetailsViewModel.MmrChange()
                    {
                        ChangeId = tableScore.TableId,
                        NewMMR = -1,
                        MmrDelta = -delta,
                        Reason = PlayerDetailsViewModel.MmrChangeReason.TableDelete,
                        Time = tableScore.Table.DeletedOn!.Value,
                    });
                }
            }

            foreach (var penalty in player.Penalties)
            {
                var newMmr = penalty.NewMmr;
                var delta = newMmr - penalty.PrevMmr;

                mmrChanges.Add(new PlayerDetailsViewModel.MmrChange()
                {
                    ChangeId = penalty.Id,
                    NewMMR = newMmr,
                    MmrDelta = delta,
                    Reason = PlayerDetailsViewModel.MmrChangeReason.Penalty,
                    Time = penalty.AwardedOn,
                });

                if (penalty.DeletedOn is not null)
                {
                    mmrChanges.Add(new PlayerDetailsViewModel.MmrChange()
                    {
                        ChangeId = penalty.Id,
                        NewMMR = -1,
                        MmrDelta = -delta,
                        Reason = PlayerDetailsViewModel.MmrChangeReason.PenaltyDelete,
                        Time = penalty.DeletedOn!.Value,
                    });
                }
            }

            mmrChanges = mmrChanges.OrderBy(c => c.Time).ToList();

            int mmr = 0;
            foreach (var change in mmrChanges)
            {
                if (change.Reason is PlayerDetailsViewModel.MmrChangeReason.TableDelete or PlayerDetailsViewModel.MmrChangeReason.PenaltyDelete)
                {
                    change.NewMMR = Math.Max(0, mmr + change.MmrDelta);
                    change.MmrDelta = change.NewMMR - mmr;
                }

                mmr = change.NewMMR;
            }

            // sort descending
            mmrChanges.Reverse();

            var vm = new PlayerDetailsViewModel
            {
                PlayerId = player.Id,
                Name = player.Name,
                MaxMmr = player.MaxMmr,
                Mmr = player.Mmr,
                MmrChanges = mmrChanges,
            };

            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
