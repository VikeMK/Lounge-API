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
using Microsoft.Extensions.Options;
using Lounge.Web.Settings;
using Lounge.Web.Controllers.ValidationAttributes;

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
        private readonly LoungeSettings _settings;

        public PlayersController(ApplicationDbContext context, IPlayerStatCache playerStatCache, IPlayerStatService playerStatService, IOptionsSnapshot<LoungeSettings> options)
        {
            _context = context;
            _playerStatCache = playerStatCache;
            _playerStatService = playerStatService;
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PlayerViewModel>> GetPlayer(string? name, int? mkcId, string? discordId, [ValidSeason]int? season = null)
        {
            season ??= _settings.Season;

            Player player;
            if (name is not null)
            {
                player = await GetPlayerByNameAsync(name);
            }
            else if (mkcId is not null)
            {
                player = await GetPlayerByMKCIdAsync(mkcId.Value);
            }
            else if (discordId is not null)
            {
                player = await GetPlayerByDiscordIdAsync(discordId);
            }
            else
            {
                return BadRequest("Must provide name, MKC ID, or discord ID");
            }

            if (player is null)
                return NotFound();

            var seasonData = player.SeasonData.FirstOrDefault(s => s.Season == season);

            return PlayerUtils.GetPlayerViewModel(player, seasonData);
        }

        [HttpGet("details")]
        [AllowAnonymous]
        public async Task<ActionResult<PlayerDetailsViewModel>> Details(string name, [ValidSeason] int? season = null)
        {
            season ??= _settings.Season;

            var player = await _context.Players
                .AsNoTracking()
                .SelectPropertiesForPlayerDetails(season.Value)
                .FirstOrDefaultAsync(p => p.NormalizedName == PlayerUtils.NormalizeName(name));

            if (player is null)
                return NotFound();

            var playerStat = await GetPlayerStatsAsync(player.Id, season.Value);
            if (playerStat is null)
                return NotFound();

            return PlayerUtils.GetPlayerDetails(player, playerStat, season.Value);
        }

        [HttpGet("list")]
        [AllowAnonymous]
        public async Task<ActionResult<PlayerListViewModel>> Players(int? minMmr, int? maxMmr, [ValidSeason] int? season=null)
        {
            season ??= _settings.Season;

            var players = await _context.PlayerSeasonData
                .Where(p => p.Season == season && (minMmr == null || p.Mmr >= minMmr) && (maxMmr == null || p.Mmr <= maxMmr))
                .Select(p => new PlayerListViewModel.Player(p.Player.Name, p.Player.MKCId, p.Mmr))
                .ToListAsync();

            return new PlayerListViewModel { Players = players };
        }

        [HttpPost("create")]
        public async Task<ActionResult<PlayerViewModel>> Create(string name, int mkcId, int? mmr, string? discordId = null)
        {
            var season = _settings.Season;

            Player player = new() { Name = name, NormalizedName = PlayerUtils.NormalizeName(name), MKCId = mkcId, DiscordId = discordId };
            PlayerSeasonData? seasonData = null;
            if (mmr is int mmrValue)
            {
                seasonData = new() { Mmr = mmrValue, Season = season };
                player.SeasonData = new List<PlayerSeasonData> { seasonData };
                Placement placement = new() { Mmr = mmrValue, PrevMmr = null, AwardedOn = DateTime.UtcNow, Season = season };
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

                var discordIdMatchExists = await _context.Players.AnyAsync(p => p.DiscordId == player.DiscordId);
                if (discordIdMatchExists)
                    return BadRequest("User with that Discord ID already exists");

                throw;
            }

            var vm = PlayerUtils.GetPlayerViewModel(player, seasonData);
            return CreatedAtAction(nameof(GetPlayer), new { name = player.Name }, vm);
        }

        [HttpPost("placement")]
        public async Task<ActionResult<PlayerViewModel>> Placement(string name, int mmr, bool force=false)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            var season = _settings.Season;
            var seasonData = player.SeasonData.FirstOrDefault(s => s.Season == season);

            if (seasonData is not null && !force)
            {
                // only look at events that have been verified and aren't deleted
                var eventsPlayed = await _context.TableScores
                    .CountAsync(s => s.PlayerId == player.Id && s.Table.VerifiedOn != null && s.Table.DeletedOn == null && s.Table.Season == season);

                if (eventsPlayed > 0)
                    return BadRequest("Player already has been placed and has played a match.");
            }

            Placement placement = new() { Mmr = mmr, PrevMmr = seasonData?.Mmr, AwardedOn = DateTime.UtcNow, PlayerId = player.Id, Season = season };
            _context.Placements.Add(placement);

            if (seasonData is null)
            {
                PlayerSeasonData newSeasonData = new() { Mmr = mmr, Season = season, PlayerId = player.Id };
                _context.PlayerSeasonData.Add(newSeasonData);
            }
            else
            {
                seasonData.Mmr = mmr;
            }

            await _context.SaveChangesAsync();

            var vm = PlayerUtils.GetPlayerViewModel(player, seasonData);

            return CreatedAtAction(nameof(GetPlayer), new { name = player.Name }, vm);
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

        [HttpPost("update/discordId")]
        public async Task<IActionResult> ChangeDiscordId(string name, string newDiscordId)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            player.DiscordId = newDiscordId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private Task<Player> GetPlayerByNameAsync(string name) =>
            _context.Players.Include(p => p.SeasonData).SingleOrDefaultAsync(p => p.NormalizedName == PlayerUtils.NormalizeName(name));

        private Task<Player> GetPlayerByMKCIdAsync(int mkcId) =>
            _context.Players.Include(p => p.SeasonData).SingleOrDefaultAsync(p => p.MKCId == mkcId);

        private Task<Player> GetPlayerByDiscordIdAsync(string discordId) =>
            _context.Players.Include(p => p.SeasonData).SingleOrDefaultAsync(p => p.DiscordId == discordId);

        private async Task<RankedPlayerStat?> GetPlayerStatsAsync(int id, int season)
        {
            RankedPlayerStat? playerStat = null;
            if (id != -1)
            {
                if (!_playerStatCache.TryGetPlayerStatsById(id, season, out playerStat))
                {
                    var stat = await _playerStatService.GetPlayerStatsByIdAsync(id, season);
                    if (stat is not null)
                    {
                        _playerStatCache.UpdatePlayerStats(stat, season);
                        _playerStatCache.TryGetPlayerStatsById(id, season, out playerStat);
                    }
                }
            }

            return playerStat;
        }
    }
}
