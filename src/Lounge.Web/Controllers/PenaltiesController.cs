﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.AspNetCore.Authorization;
using Lounge.Web.Data;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Lounge.Web.Utils;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Settings;
using System.Linq;
using Lounge.Web.Data.Entities;
using Lounge.Web.Models.Enums;

namespace Lounge.Web.Controllers
{
    [Route("api/penalty")]
    [Authorize]
    [ApiController]
    public class PenaltiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILoungeSettingsService _loungeSettingsService;

        public PenaltiesController(ApplicationDbContext context, ILoungeSettingsService loungeSettingsService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _loungeSettingsService = loungeSettingsService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PenaltyViewModel>> GetPenalty(int id)
        {
            var penaltyData = await _context.Penalties
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new { Penalty = p, PlayerName = p.Player.Name })
                .FirstOrDefaultAsync();

            if (penaltyData is null)
                return NotFound();

            return PenaltyUtils.GetPenaltyDetails(penaltyData.Penalty, penaltyData.PlayerName);
        }

        [HttpGet("list")]
        [AllowAnonymous]
        public async Task<ActionResult<List<PenaltyViewModel>>> GetPenalties(
            string name,
            bool? isStrike = null,
            DateTime? from = null,
            bool includeDeleted = false,
            Game game = Game.mk8dx,
            int? season = null)
        {
            if (season != null && !_loungeSettingsService.ValidSeasons[game].Contains(season.Value))
                return BadRequest($"Invalid season {season} for game {game}");

            season ??= _loungeSettingsService.CurrentSeason[game];

            var player = await _context.PlayerGameRegistrations
                .AsNoTracking()
                .Where(pgr => pgr.Game == (int)game)
                .Select(pgr => pgr.Player)
                .Where(p => p.NormalizedName == PlayerUtils.NormalizeName(name))
                .Select(p => new
                {
                    Name = p.Name,
                    Penalties = p.Penalties
                        .Where(pen => pen.Season == season && pen.Game == (int)game)
                        .Where(pen => includeDeleted || pen.DeletedOn == null)
                        .Where(pen => isStrike == null || pen.IsStrike == isStrike)
                        .Where(pen => from == null || pen.AwardedOn >= from)
                })
                .SingleOrDefaultAsync();

            if (player is null)
                return NotFound();

            return player.Penalties.Select(p => PenaltyUtils.GetPenaltyDetails(p, player.Name)).ToList();
        }

        [HttpPost("create")]
        public async Task<ActionResult<PenaltyViewModel>> Penalise(string name, int amount, bool isStrike, Game game = Game.mk8dx)
        {
            if (amount < 0)
                return BadRequest("Penalty amount must be a non-negative integer");

            var season = _loungeSettingsService.CurrentSeason[game];
            var player = await _context.Players
                .Where(p => p.NormalizedName == PlayerUtils.NormalizeName(name))
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
                return BadRequest("Player has not been placed yet, so penalty can't be given");

            int prevMmr = seasonData.Mmr;
            var newMmr = Math.Max(0, prevMmr - amount);

            Penalty penalty = new()
            {
                AwardedOn = DateTime.UtcNow,
                IsStrike = isStrike,
                PrevMmr = prevMmr,
                NewMmr = newMmr,
                Game = (int)game,
                Season = season,
                PlayerId = player.Id,
            };

            seasonData.Mmr = newMmr;

            // no need to update max mmr, since a penalty will only ever decrease their MMR

            _context.Penalties.Add(penalty);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPenalty), new { id = penalty.Id }, PenaltyUtils.GetPenaltyDetails(penalty, player.Name));
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id, Game game)
        {
            var penaltyData = await _context.Penalties
                .Where(p => p.Id == id && p.Game == (int)game)
                .Select(p => new { Penalty = p, SeasonData = p.Player.SeasonData.Single(s => s.Season == p.Season && s.Game == p.Game) })
                .FirstOrDefaultAsync();

            if (penaltyData is null)
                return NotFound();

            var penalty = penaltyData.Penalty;
            var seasonData = penaltyData.SeasonData;

            if (penalty.DeletedOn is not null)
                return BadRequest("Penalty has already been deleted");

            if (penalty.Season != _loungeSettingsService.CurrentSeason[(Game)seasonData.Game])
                return BadRequest("Penalty is from a previous season and can't be deleted");

            penalty.DeletedOn = DateTime.UtcNow;

            var curMMR = seasonData.Mmr;
            var diff = penalty.NewMmr - penalty.PrevMmr;
            var newMmr = Math.Max(0, curMMR - diff);

            seasonData.Mmr = newMmr;
            if (seasonData.MaxMmr is int maxMMR)
            {
                seasonData.MaxMmr = Math.Max(maxMMR, newMmr);
            }

            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
