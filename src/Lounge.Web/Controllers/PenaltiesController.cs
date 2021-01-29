using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System;
using Lounge.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Lounge.Web.Data;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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

            return player.Penalties.ToList();
        }

        [HttpPost("create")]
        public async Task<ActionResult<Penalty>> Penalise(string name, int amount, bool isStrike)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            if (player.Mmr is null)
                return BadRequest("Player has not been placed yet, so penalty can't be given");

            int prevMMR = player.Mmr.Value;

            if (amount < 0)
                return BadRequest("Penalty amount must be a non-negative integer");

            Penalty penalty = new()
            {
                AwardedOn = DateTime.UtcNow,
                IsStrike = isStrike,
                PrevMMR = prevMMR,
                NewMMR = Math.Max(0, prevMMR - amount),
            };

            player.Penalties.Add(penalty);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPenalty), new { id = penalty.Id }, penalty);
        }

        private Task<Player> GetPlayerByNameAsync(string name) =>
            _context.Players.Include(p => p.Penalties).SingleOrDefaultAsync(p => p.NormalizedName == name.ToUpperInvariant());
    }
}
