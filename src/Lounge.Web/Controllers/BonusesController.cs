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
using System.Linq;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Controllers.ValidationAttributes;

namespace Lounge.Web.Controllers
{
    [Route("api/bonus")]
    [Authorize]
    [ApiController]
    public class BonusesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly LoungeSettings _settings;

        public BonusesController(ApplicationDbContext context, IOptionsSnapshot<LoungeSettings> options)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<BonusViewModel>> GetBonus(int id)
        {
            var bonusData = await _context.Bonuses
                .AsNoTracking()
                .Where(b => b.Id == id)
                .Select(b => new { Bonus = b, PlayerName = b.Player.Name })
                .FirstOrDefaultAsync();

            if (bonusData is null)
                return NotFound();

            return BonusUtils.GetBonusDetails(bonusData.Bonus, bonusData.PlayerName);
        }

        [HttpGet("list")]
        [AllowAnonymous]
        public async Task<ActionResult<List<BonusViewModel>>> GetBonuses(string name, [ValidSeason] int? season = null)
        {
            season ??= _settings.Season;

            var player = await _context.Players
                .AsNoTracking()
                .Where(p => p.NormalizedName == PlayerUtils.NormalizeName(name))
                .Select(p => new { Name = p.Name, Bonuses = p.Bonuses.Where(b => b.Season == season) })
                .SingleOrDefaultAsync();

            if (player is null)
                return NotFound();

            return player.Bonuses.Select(b => BonusUtils.GetBonusDetails(b, b.Player.Name)).ToList();
        }

        [HttpPost("create")]
        public async Task<ActionResult<BonusViewModel>> AwardBonus(string name, int amount)
        {
            if (amount < 0)
                return BadRequest("Bonus amount must be a non-negative integer");

            var season = _settings.Season;
            var player = await _context.Players
                .Where(p => p.NormalizedName == PlayerUtils.NormalizeName(name))
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    CurrentSeasonData = p.SeasonData.FirstOrDefault(s => s.Season == season),
                })
                .SingleOrDefaultAsync();

            if (player is null)
                return NotFound();

            var seasonData = player.CurrentSeasonData;
            if (seasonData is null)
                return BadRequest("Player has not been placed yet, so bonus can't be given");

            int prevMmr = seasonData.Mmr;
            var newMmr = prevMmr + amount;

            Bonus bonus = new()
            {
                AwardedOn = DateTime.UtcNow,
                PrevMmr = prevMmr,
                NewMmr = newMmr,
                Season = season,
                PlayerId = player.Id
            };

            seasonData.Mmr = newMmr;
            if (seasonData.MaxMmr is int maxMMR)
            {
                seasonData.MaxMmr = Math.Max(maxMMR, newMmr);
            }

            _context.Bonuses.Add(bonus);

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBonus), new { id = bonus.Id }, BonusUtils.GetBonusDetails(bonus, player.Name));
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var bonusData = await _context.Bonuses
                .Where(b => b.Id == id)
                .Select(b => new { Bonus = b, SeasonData = b.Player.SeasonData.Single(s => s.Season == b.Season) })
                .FirstOrDefaultAsync();

            if (bonusData is null)
                return NotFound();

            var bonus = bonusData.Bonus;
            var seasonData = bonusData.SeasonData;

            if (bonus.DeletedOn is not null)
                return BadRequest("Bonus has already been deleted");

            bonus.DeletedOn = DateTime.UtcNow;

            var curMMR = seasonData.Mmr;
            var diff = bonus.NewMmr - bonus.PrevMmr;
            var newMmr = Math.Max(0, curMMR - diff);

            seasonData.Mmr = newMmr;

            // no need to update max mmr since deleting a bonus will only cause their MMR to decrease

            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
