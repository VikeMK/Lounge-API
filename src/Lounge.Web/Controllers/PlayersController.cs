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

            var playerStat = await _context.PlayerStats.FirstOrDefaultAsync(p => p.Id == player.Id);

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
            if (mmr is not null)
            {
                player.PlacedOn = DateTime.UtcNow;
                player.InitialMmr = mmr;
                player.Mmr = mmr;
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
            {
                var eventsPlayed = await _context.Players
                    .Where(p => p.Id == player.Id)
                    .Select(t => t.TableScores.Count())
                    .FirstOrDefaultAsync();

                if (eventsPlayed > 0)
                    return BadRequest("Player already has been placed and has played a match.");
            }

            player.PlacedOn = DateTime.UtcNow;
            player.InitialMmr = mmr;
            player.Mmr = mmr;

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

        private Task<Player> GetPlayerByMKCIdAsync(int mkcId) =>
            _context.Players.SingleOrDefaultAsync(p => p.MKCId == mkcId);
    }
}
