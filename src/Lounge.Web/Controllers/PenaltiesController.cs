using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System;
using Lounge.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Lounge.Web.Data;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Lounge.Web.Utils;

namespace Lounge.Web.Controllers
{
    [Route("api/penalty")]
    [Authorize]
    [ApiController]
    public class PenaltiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PenaltiesController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [HttpGet]
        public async Task<ActionResult<Penalty>> GetPenalty(int id)
        {
            var penalty = await _context.Penalties.FindAsync(id);
            if (penalty is null)
                return NotFound();

            return penalty;
        }

        [HttpGet("list")]
        public async Task<ActionResult<List<Penalty>>> GetPenalties(string name)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            var penalties = new List<Penalty>();
            foreach (var penalty in player.Penalties)
            {
                penalty.Player = null!;
                penalties.Add(penalty);
            }

            return penalties;
        }

        [HttpPost("create")]
        public async Task<ActionResult<Penalty>> Penalise(string name, int amount, bool isStrike)
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
            };

            player.Mmr = newMmr;
            player.MaxMmr = Math.Max(player.MaxMmr!.Value, player.Mmr!.Value);

            player.Penalties.Add(penalty);
            await _context.SaveChangesAsync();

            penalty.Player = null!;

            return CreatedAtAction(nameof(GetPenalty), new { id = penalty.Id }, penalty);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var penalty = await _context.Penalties.Include(p => p.Player).FirstOrDefaultAsync(p => p.Id == id);
            if (penalty is null)
                return NotFound();

            if (penalty.DeletedOn is not null)
                return BadRequest("Table has already been deleted");

            penalty.DeletedOn = DateTime.UtcNow;

            var curMMR = penalty.Player.Mmr!.Value;
            var diff = penalty.NewMmr - penalty.PrevMmr;
            var newMMR = Math.Max(0, curMMR - diff);
            penalty.Player.Mmr = newMMR;
            penalty.Player.MaxMmr = Math.Max(penalty.Player.MaxMmr!.Value, newMMR);

            await _context.SaveChangesAsync();

            return Ok();
        }

        private Task<Player> GetPlayerByNameAsync(string name) =>
            _context.Players.Include(p => p.Penalties).SingleOrDefaultAsync(p => p.NormalizedName == PlayerUtils.NormalizeName(name));
    }
}
