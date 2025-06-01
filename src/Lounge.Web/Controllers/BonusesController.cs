using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.AspNetCore.Authorization;
using Lounge.Web.Data;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Lounge.Web.Utils;
using Lounge.Web.Settings;
using System.Linq;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Controllers.ValidationAttributes;
using Lounge.Web.Data.Entities;
using Lounge.Web.Models.Enums;

namespace Lounge.Web.Controllers
{
    [Route("api/bonus")]
    [Authorize]
    [ApiController]
    public class BonusesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILoungeSettingsService _loungeSettingsService;

        public BonusesController(ApplicationDbContext context, ILoungeSettingsService loungeSettingsService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _loungeSettingsService = loungeSettingsService;
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
        public async Task<ActionResult<List<BonusViewModel>>> GetBonuses(string name, Game game = Game.MK8DX, [ValidSeason]int? season = null)
        {
            season ??= _loungeSettingsService.CurrentSeason[game];

            var player = await _context.Players
                .AsNoTracking()
                .Where(p => p.NormalizedName == PlayerUtils.NormalizeName(name))
                .Select(p => new { Name = p.Name, Bonuses = p.Bonuses.Where(b => b.Season == season && b.Game == (int)game) })
                .SingleOrDefaultAsync();

            if (player is null)
                return NotFound();

            return player.Bonuses.Select(b => BonusUtils.GetBonusDetails(b, b.Player.Name)).ToList();
        }

        [HttpPost("create")]
        public async Task<ActionResult<BonusViewModel>> AwardBonus(string? name, int? mkcId, int amount, Game game = Game.MK8DX)
        {
            if (amount < 0)
                return BadRequest("Bonus amount must be a non-negative integer");

            var season = _loungeSettingsService.CurrentSeason[game];
            var player = await _context.Players
                .Where(p => 
                    (name == null || p.NormalizedName == PlayerUtils.NormalizeName(name)) && 
                    (mkcId == null || p.RegistryId == mkcId.Value))
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    CurrentSeasonData = p.SeasonData.FirstOrDefault(s => s.Season == season && s.Game == (int)game),
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
                Game = (int)game,
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
        public async Task<IActionResult> Delete(int id, Game game)
        {
            var bonusData = await _context.Bonuses
                .Where(b => b.Id == id && b.Game == (int)game)
                .Select(b => new { Bonus = b, SeasonData = b.Player.SeasonData.Single(s => s.Season == b.Season && s.Game == b.Game) })
                .FirstOrDefaultAsync();

            if (bonusData is null)
                return NotFound();

            var bonus = bonusData.Bonus;
            var seasonData = bonusData.SeasonData;

            if (bonus.DeletedOn is not null)
                return BadRequest("Bonus has already been deleted");

            if (bonus.Season != _loungeSettingsService.CurrentSeason[(Game)seasonData.Game])
                return BadRequest("Bonus is from a previous season and can't be deleted");

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
