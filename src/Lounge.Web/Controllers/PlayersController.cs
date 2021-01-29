using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Lounge.Web.Models;
using Lounge.Web.Data;
using Microsoft.AspNetCore.Authorization;

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
        public async Task<ActionResult<Player>> GetPlayer(string name)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            return player;
        }

        [HttpPost("create")]
        public async Task<ActionResult<Player>> Create(string name, int mkcId, int? mmr)
        {
            Player player = new() { Name = name, NormalizedName = name.ToUpperInvariant(), MKCId = mkcId };
            if (mmr is not null)
            {
                player.PlacedOn = DateTime.UtcNow;
                player.InitialMmr = mmr;
                player.Mmr = mmr;
                player.MaxMmr = mmr;
            }

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPlayer), new { name = player.Name }, player);
        }

        [HttpPost("placement")]
        public async Task<ActionResult<Player>> Create(string name, int mmr)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            if (player.Mmr is not null)
                return BadRequest("Player already has been placed.");

            player.PlacedOn = DateTime.UtcNow;
            player.InitialMmr = mmr;
            player.Mmr = mmr;
            player.MaxMmr = mmr;

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
            player.NormalizedName = newName.ToUpperInvariant();
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("update/mkcId")]
        public async Task<IActionResult> ChangeMkcId(string name, int newMkcId)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            player.MKCId = newMkcId;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private Task<Player> GetPlayerByNameAsync(string name) =>
            _context.Players.SingleOrDefaultAsync(p => p.NormalizedName == name.ToUpperInvariant());
    }
}
