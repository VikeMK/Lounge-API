using Lounge.Web.Data;
using Lounge.Web.Data.Entities;
using Lounge.Web.Models.Enums;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Settings;
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
        public async Task<ActionResult<List<BonusViewModel>>> GetBonuses(string name, GameMode game = GameMode.mk8dx, int? season = null)
        {
            if (!_loungeSettingsService.ValidateGameAndSeason(ref game, ref season, out var error, allowMkWorldFallback: false))
                return BadRequest(error);

            var player = await _context.PlayerGameRegistrations
                .AsNoTracking()
                .Where(pgr => pgr.Game == game.GetRegistrationGameMode())
                .Select(pgr => pgr.Player)
                .Where(p => p.NormalizedName == PlayerUtils.NormalizeName(name))
                .Select(p => new { Name = p.Name, Bonuses = p.Bonuses.Where(b => b.Season == season && b.Game == game) })
                .SingleOrDefaultAsync();

            if (player is null)
                return NotFound();

            return player.Bonuses.Select(b => BonusUtils.GetBonusDetails(b, b.Player.Name)).ToList();
        }

        [HttpPost("create")]
        public async Task<ActionResult<BonusViewModel>> AwardBonus(string? name, int? mkcId, int amount, GameMode game = GameMode.mk8dx)
        {
            if (!_loungeSettingsService.ValidateCurrentGame(ref game, out var currentSeason, out var error, allowMkWorldFallback: false))
                return BadRequest(error);

            if (amount < 0)
                return BadRequest("Bonus amount must be a non-negative integer");

            var player = await _context.PlayerGameRegistrations
                .Where(pgr => pgr.Game == game.GetRegistrationGameMode())
                .Select(pgr => pgr.Player)
                .Where(p => 
                    (name == null || p.NormalizedName == PlayerUtils.NormalizeName(name)) && 
                    (mkcId == null || p.RegistryId == mkcId.Value))
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    CurrentSeasonData = p.SeasonData.FirstOrDefault(s => s.Season == currentSeason && s.Game == game),
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
                Game = game,
                Season = currentSeason.Value,
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
        public async Task<IActionResult> Delete(int id, GameMode game)
        {
            var bonusData = await _context.Bonuses
                .Where(b => b.Id == id)
                .Select(b => new { Bonus = b, SeasonData = b.Player.SeasonData.Single(s => s.Season == b.Season && s.Game == b.Game) })
                .FirstOrDefaultAsync();

            if (bonusData is null)
                return NotFound();

            var bonus = bonusData.Bonus;
            var seasonData = bonusData.SeasonData;

            if (!_loungeSettingsService.ValidateGameMatchesAndFromCurrentSeason(game, bonus.Season, bonus.Game, out var error))
                return BadRequest(error);

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
