using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Lounge.Web.Models;
using Lounge.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Lounge.Web.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using Lounge.Web.Utils;

namespace Lounge.Web.Controllers
{
    [Route("api/player")]
    [Authorize]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PlayersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<Player>> GetPlayer(string name)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            return player;
        }

        [HttpGet("details")]
        public async Task<ActionResult<PlayerDetailsViewModel>> Details(string name)
        {
            var player = await _context.Players
                .Include(p => p.Penalties)
                .Include(p => p.TableScores)
                    .ThenInclude(s => s.Table)
                .FirstOrDefaultAsync(p => p.NormalizedName == PlayerUtils.NormalizeName(name));
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
                MkcId = player.MKCId,
                MaxMmr = player.MaxMmr,
                Mmr = player.Mmr,
                MmrChanges = mmrChanges,
            };

            return vm;
        }

        [HttpPost("create")]
        public async Task<ActionResult<Player>> Create(string name, int mkcId, int? mmr)
        {
            Player player = new() { Name = name, NormalizedName = PlayerUtils.NormalizeName(name), MKCId = mkcId };
            if (mmr is not null)
            {
                player.PlacedOn = DateTime.UtcNow;
                player.InitialMmr = mmr;
                player.Mmr = mmr;
                player.MaxMmr = mmr;
            }

            _context.Players.Add(player);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                var nameMatchExists = await _context.Players.AnyAsync(p => p.NormalizedName == player.NormalizedName);
                if (nameMatchExists)
                    return BadRequest("User with that name already exists");

                var mkcIdMatchExists = await _context.Players.AnyAsync(p => p.MKCId == player.MKCId);
                if (mkcIdMatchExists)
                    return BadRequest("User with that MKC ID already exists");

                throw;
            }

            return CreatedAtAction(nameof(GetPlayer), new { name = player.Name }, player);
        }

        [HttpPost("placement")]
        public async Task<ActionResult<Player>> Placement(string name, int mmr)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            if (player.Mmr is not null)
                return BadRequest("Player already has been placed.");

            player.PlacedOn = DateTime.UtcNow;
            player.InitialMmr = mmr;
            player.Mmr = mmr;
            player.MaxMmr = mmr;

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPlayer), new { name = player.Name }, player);
        }

        [HttpPost("update/name")]
        public async Task<IActionResult> ChangeName(string name, string newName)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            player.Name = newName;
            player.NormalizedName = PlayerUtils.NormalizeName(newName);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                var nameMatchExists = await _context.Players.AnyAsync(p => p.NormalizedName == player.NormalizedName);
                if (nameMatchExists)
                    return BadRequest("User with that name already exists");

                throw;
            }

            return NoContent();
        }

        [HttpPost("update/mkcId")]
        public async Task<IActionResult> ChangeMkcId(string name, int newMkcId)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            player.MKCId = newMkcId;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                var mkcIdMatchExists = await _context.Players.AnyAsync(p => p.MKCId == player.MKCId);
                if (mkcIdMatchExists)
                    return BadRequest("User with that MKC ID already exists");

                throw;
            }

            return NoContent();
        }

        private Task<Player> GetPlayerByNameAsync(string name) =>
            _context.Players.SingleOrDefaultAsync(p => p.NormalizedName == PlayerUtils.NormalizeName(name));
    }
}
