using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System;
using Lounge.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Lounge.Web.Data;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Lounge.Web.Utils;
using Lounge.Web.Settings;
using Microsoft.Extensions.Options;

namespace Lounge.Web.Controllers
{
    [Route("api/bonus")]
    [Authorize]
    [ApiController]
    public class BonusesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOptionsMonitor<LoungeSettings> options;

        public BonusesController(ApplicationDbContext context, IOptionsMonitor<LoungeSettings> options)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            this.options = options;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<Bonus>> GetBonus(int id)
        {
            var bonus = await _context.Bonuses.FindAsync(id);
            if (bonus is null)
                return NotFound();

            return bonus;
        }

        [HttpGet("list")]
        [AllowAnonymous]
        public async Task<ActionResult<List<Bonus>>> GetBonuses(string name)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            var bonuses = new List<Bonus>();
            foreach (var bonus in player.Bonuses)
            {
                bonus.Player = null!;
                bonuses.Add(bonus);
            }

            return bonuses;
        }

        [HttpPost("create")]
        public async Task<ActionResult<Bonus>> AwardBonus(string name, int amount)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            if (player.Mmr is null)
                return BadRequest("Player has not been placed yet, so bonus can't be given");

            int prevMmr = player.Mmr.Value;

            if (amount < 0)
                return BadRequest("Bonus amount must be a non-negative integer");

            var newMmr = prevMmr + amount;

            Bonus bonus = new()
            {
                AwardedOn = DateTime.UtcNow,
                PrevMmr = prevMmr,
                NewMmr = newMmr,
                Season = options.CurrentValue.Season,
            };

            player.Mmr = newMmr;
            if (player.MaxMmr is int maxMMR)
            {
                player.MaxMmr = Math.Max(maxMMR, newMmr);
            }

            player.Bonuses.Add(bonus);
            await _context.SaveChangesAsync();

            bonus.Player = null!;

            return CreatedAtAction(nameof(GetBonus), new { id = bonus.Id }, bonus);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var bonus = await _context.Bonuses.Include(p => p.Player).FirstOrDefaultAsync(p => p.Id == id);
            if (bonus is null)
                return NotFound();

            if (bonus.DeletedOn is not null)
                return BadRequest("Bonus has already been deleted");

            bonus.DeletedOn = DateTime.UtcNow;

            var curMMR = bonus.Player.Mmr!.Value;
            var diff = bonus.NewMmr - bonus.PrevMmr;
            var newMmr = Math.Max(0, curMMR - diff);

            bonus.Player.Mmr = newMmr;

            // no need to update max mmr since deleting a bonus will only cause their MMR to decrease

            await _context.SaveChangesAsync();

            return Ok();
        }

        private Task<Player> GetPlayerByNameAsync(string name) =>
            _context.Players.Include(p => p.Bonuses).SingleOrDefaultAsync(p => p.NormalizedName == PlayerUtils.NormalizeName(name));
    }
}
