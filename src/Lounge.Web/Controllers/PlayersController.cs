using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Lounge.Web.Models;
using Lounge.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Utils;
using System.Linq;
using Lounge.Web.Stats;
using System.Collections.Generic;

namespace Lounge.Web.Controllers
{
    [Route("api/player")]
    [Authorize]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPlayerStatCache _playerStatCache;
        private readonly IPlayerStatService _playerStatService;

        public PlayersController(ApplicationDbContext context, IPlayerStatCache playerStatCache, IPlayerStatService playerStatService)
        {
            _context = context;
            _playerStatCache = playerStatCache;
            _playerStatService = playerStatService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<Player>> GetPlayer(string? name, int? mkcId)
        {
            Player player;
            if (name is not null)
            {
                player = await GetPlayerByNameAsync(name);
            }
            else if (mkcId is not null)
            {
                player = await GetPlayerByMKCIdAsync(mkcId.Value);
            }
            else
            {
                return BadRequest("Must provide name or MKC ID");
            }

            if (player is null)
                return NotFound();

            return player;
        }

        [HttpGet("details")]
        [AllowAnonymous]
        public async Task<ActionResult<PlayerDetailsViewModel>> Details(string name)
        {
            var player = await _context.Players
                .AsNoTracking()
                .SelectPropertiesForPlayerDetails()
                .FirstOrDefaultAsync(p => p.NormalizedName == PlayerUtils.NormalizeName(name));

            if (player is null)
                return NotFound();

            var playerStat = await GetPlayerStatsAsync(player.Id);
            if (playerStat is null)
                return NotFound();

            return PlayerUtils.GetPlayerDetails(player, playerStat);
        }

        [HttpGet("list")]
        [AllowAnonymous]
        public async Task<PlayerListViewModel> Players(int? minMmr, int? maxMmr)
        {
            var players = await _context.Players
                .Where(p => (minMmr == null || (p.Mmr != null && p.Mmr >= minMmr)) && (maxMmr == null || (p.Mmr != null && p.Mmr <= maxMmr)))
                .Select(p => new PlayerListViewModel.Player(p.Name, p.MKCId, p.Mmr))
                .ToListAsync();

            return new PlayerListViewModel { Players = players };
        }

        [HttpPost("create")]
        public async Task<ActionResult<Player>> Create(string name, int mkcId, int? mmr)
        {
            Player player = new() { Name = name, NormalizedName = PlayerUtils.NormalizeName(name), MKCId = mkcId };
            if (mmr is int mmrValue)
            {
                player.Mmr = mmrValue;
                Placement placement = new() { Mmr = mmrValue, PrevMmr = null, AwardedOn = DateTime.UtcNow };
                player.Placements = new List<Placement> { placement };
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

            if (player.Placements is not null)
            {
                foreach (var placement in player.Placements)
                {
                    placement.Player = default!;
                }
            }

            return CreatedAtAction(nameof(GetPlayer), new { name = player.Name }, player);
        }

        [HttpPost("placement")]
        public async Task<ActionResult<Player>> Placement(string name, int mmr, bool force=false)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            if (player.Mmr is not null && !force)
            {
                // only look at events that have been verified and aren't deleted
                var eventsPlayed = await _context.Players
                    .Where(p => p.Id == player.Id)
                    .Select(t => t.TableScores.Count(s => s.Table.VerifiedOn != null && s.Table.DeletedOn == null))
                    .FirstOrDefaultAsync();

                if (eventsPlayed > 0)
                    return BadRequest("Player already has been placed and has played a match.");
            }

            Placement placement = new() { Mmr = mmr, PrevMmr = player.Mmr, AwardedOn = DateTime.UtcNow, PlayerId = player.Id };
            _context.Placements.Add(placement);

            player.Mmr = mmr;

            await _context.SaveChangesAsync();

            player.Placements = default!;

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

        private Task<Player> GetPlayerByMKCIdAsync(int mkcId) =>
            _context.Players.SingleOrDefaultAsync(p => p.MKCId == mkcId);

        private async Task<RankedPlayerStat?> GetPlayerStatsAsync(int id)
        {
            RankedPlayerStat? playerStat = null;
            if (id != -1)
            {
                if (!_playerStatCache.TryGetPlayerStatsById(id, out playerStat))
                {
                    var stat = await _playerStatService.GetPlayerStatsByIdAsync(id);
                    if (stat is not null)
                    {
                        _playerStatCache.UpdatePlayerStats(stat);
                        _playerStatCache.TryGetPlayerStatsById(id, out playerStat);
                    }
                }
            }

            return playerStat;
        }
    }
}
