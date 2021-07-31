using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System;
using Lounge.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Lounge.Web.Data;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Lounge.Web.Utils;
using Lounge.Web.Models.ViewModels;
using Microsoft.Extensions.Options;
using Lounge.Web.Settings;

namespace Lounge.Web.Controllers
{
    [Route("api/penalty")]
    [Authorize]
    [ApiController]
    public class PenaltiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOptionsMonitor<LoungeSettings> options;

        public PenaltiesController(ApplicationDbContext context, IOptionsMonitor<LoungeSettings> options)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            this.options = options;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PenaltyViewModel>> GetPenalty(int id)
        {
            var penalty = await _context.Penalties.Include(p => p.Player).FirstOrDefaultAsync(p => p.Id == id);
            if (penalty is null)
                return NotFound();

            return PenaltyUtils.GetPenaltyDetails(penalty);
        }

        [HttpGet("list")]
        [AllowAnonymous]
        public async Task<ActionResult<List<PenaltyViewModel>>> GetPenalties(string name, bool? isStrike = null, DateTime? from = null, bool includeDeleted = false)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            var penalties = new List<PenaltyViewModel>();
            foreach (var penalty in player.Penalties)
            {
                if (!includeDeleted && penalty.DeletedOn is not null)
                    continue;

                if (isStrike is not null && penalty.IsStrike != isStrike)
                    continue;

                if (from is not null && penalty.AwardedOn < from)
                    continue;

                penalties.Add(PenaltyUtils.GetPenaltyDetails(penalty));
            }

            return penalties;
        }

        [HttpPost("create")]
        public async Task<ActionResult<PenaltyViewModel>> Penalise(string name, int amount, bool isStrike)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            if (player.Mmr is null)
                return BadRequest("Player has not been placed yet, so penalty can't be given");

            int prevMmr = player.Mmr.Value;

            if (amount < 0)
                return BadRequest("Penalty amount must be a non-negative integer");

            var newMmr = Math.Max(0, prevMmr - amount);

            Penalty penalty = new()
            {
                AwardedOn = DateTime.UtcNow,
                IsStrike = isStrike,
                PrevMmr = prevMmr,
                NewMmr = newMmr,
                Season = options.CurrentValue.Season,
            };

            player.Mmr = newMmr;
            
            // no need to update max mmr, since a penalty will only ever decrease their MMR

            player.Penalties.Add(penalty);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPenalty), new { id = penalty.Id }, PenaltyUtils.GetPenaltyDetails(penalty));
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var penalty = await _context.Penalties.Include(p => p.Player).FirstOrDefaultAsync(p => p.Id == id);
            if (penalty is null)
                return NotFound();

            if (penalty.DeletedOn is not null)
                return BadRequest("Penalty has already been deleted");

            penalty.DeletedOn = DateTime.UtcNow;

            var curMMR = penalty.Player.Mmr!.Value;
            var diff = penalty.NewMmr - penalty.PrevMmr;
            var newMmr = Math.Max(0, curMMR - diff);

            penalty.Player.Mmr = newMmr;
            if (penalty.Player.MaxMmr is int maxMMR)
            {
                penalty.Player.MaxMmr = Math.Max(maxMMR, newMmr);
            }

            await _context.SaveChangesAsync();

            return Ok();
        }

        private Task<Player> GetPlayerByNameAsync(string name) =>
            _context.Players.Include(p => p.Penalties).SingleOrDefaultAsync(p => p.NormalizedName == PlayerUtils.NormalizeName(name));
    }
}
