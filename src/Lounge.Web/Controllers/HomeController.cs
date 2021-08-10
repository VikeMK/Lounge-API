using Lounge.Web.Controllers.ValidationAttributes;
using Lounge.Web.Data;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Settings;
using Lounge.Web.Stats;
using Lounge.Web.Storage;
using Lounge.Web.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lounge.Web.Controllers
{
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : Controller
    {
        private const int PageSize = 100;

        private readonly ApplicationDbContext _context;
        private readonly IPlayerStatCache _playerStatCache;
        private readonly IPlayerStatService _playerStatService;
        private readonly ITableImageService _tableImageService;
        private readonly ILoungeSettingsService _loungeSettingsService;

        public HomeController(
            ApplicationDbContext context,
            IPlayerStatCache playerStatCache,
            IPlayerStatService playerStatService,
            ITableImageService tableImageService,
            ILoungeSettingsService loungeSettingsService)
        {
            _context = context;
            _playerStatCache = playerStatCache;
            _playerStatService = playerStatService;
            _tableImageService = tableImageService;
            _loungeSettingsService = loungeSettingsService;
        }

        [ResponseCache(Duration = 180)]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Leaderboard));
        }

        [ResponseCache(Duration = 180, VaryByQueryKeys = new string[] { "*" })]
        [Route("Leaderboard")]
        public async Task<IActionResult> Leaderboard(int page = 1, string? filter = null, [ValidSeason] int? season = null, LeaderboardSortOrder sortBy = LeaderboardSortOrder.Mmr)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (page <= 0)
                return BadRequest("Page must be a number 1 or greater");

            season ??= _loungeSettingsService.CurrentSeason;

            int? playerId = null;
            if (filter != null)
            {
                if (filter.StartsWith("mkc="))
                {
                    if (int.TryParse(filter[4..], out int mkcId))
                    {
                        playerId = await _context.Players
                            .Where(p => p.MKCId == mkcId)
                            .Select(p => (int?) p.Id)
                            .FirstOrDefaultAsync();
                    }

                    // if no player exists with that MKC ID then we should show nothing so set player ID to -1
                    playerId ??= -1;
                }
                else if (filter.StartsWith("discord="))
                {
                    var discordId = filter["discord=".Length..];

                    playerId = await _context.Players
                        .Where(p => p.DiscordId == discordId)
                        .Select(p => (int?) p.Id)
                        .FirstOrDefaultAsync();

                    // if no player exists with that Discord ID then we should show nothing so set player ID to -1
                    playerId ??= -1;
                }
            }

            IReadOnlyList<RankedPlayerStat> playerEntities;
            int totalPlayerCount;
            if (playerId is int id)
            {
                RankedPlayerStat? playerStat = await GetPlayerStatsAsync(id, season.Value);
                playerEntities = playerStat == null ? Array.Empty<RankedPlayerStat>() : new[] { playerStat };
                totalPlayerCount = playerEntities.Count;
            }
            else
            {
                var leaderboard = _playerStatCache.GetAllStats(season.Value, sortBy);
                if (filter != null)
                {
                    var normalized = PlayerUtils.NormalizeName(filter);
                    leaderboard = leaderboard.Where(s => s.Stat.NormalizedName.Contains(normalized)).ToList();
                }

                playerEntities = leaderboard
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
                totalPlayerCount = leaderboard.Count;
            }

            // clever trick to get number of pages https://stackoverflow.com/a/503201
            var maxPageNum = (totalPlayerCount - 1) / PageSize + 1;
            page = Math.Clamp(page, 1, maxPageNum);

            var playerViewModels = new List<LeaderboardViewModel.Player>();
            foreach (var rankedPlayerStat in playerEntities)
            {
                var playerStat = rankedPlayerStat.Stat;
                decimal? winRate = playerStat.EventsPlayed == 0 ? null : (decimal)playerStat.Wins / playerStat.EventsPlayed;

                playerViewModels.Add(new LeaderboardViewModel.Player
                {
                    Id = playerStat.Id,
                    OverallRank = playerStat.EventsPlayed == 0 ? null : rankedPlayerStat.Rank,
                    Name = playerStat.Name,
                    Mmr = playerStat.Mmr,
                    MaxMmr = playerStat.MaxMmr,
                    EventsPlayed = playerStat.EventsPlayed,
                    WinRate = winRate,
                    WinsLastTen = playerStat.LastTenWins,
                    LossesLastTen = playerStat.LastTenLosses,
                    GainLossLastTen = playerStat.LastTenGainLoss,
                    LargestGain = playerStat.LargestGain < 0 ? null : playerStat.LargestGain,
                    LargestLoss = playerStat.LargestLoss > 0 ? null : playerStat.LargestLoss,
                    MmrRank = _loungeSettingsService.GetRank(playerStat.Mmr, season.Value),
                    MaxMmrRank = _loungeSettingsService.GetRank(playerStat.MaxMmr, season.Value)
                });
            }

            return View(new LeaderboardViewModel
            {
                Players = playerViewModels,
                Season = season.Value,
                Page = page,
                HasNextPage = page < maxPageNum,
                HasPrevPage = page > 1,
                Filter = filter,
                ValidSeasons = _loungeSettingsService.ValidSeasons,
                SortColumn = sortBy,
            });
        }

        [ResponseCache(Duration = 180, VaryByQueryKeys = new string[] { "*" })]
        [Route("PlayerDetails/{id}")]
        public async Task<IActionResult> PlayerDetails(int id, [ValidSeason] int? season=null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            season ??= _loungeSettingsService.CurrentSeason;

            var player = await _context.Players
                .AsNoTracking()
                .SelectPropertiesForPlayerDetails(season.Value)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (player is null)
                return NotFound();

            var playerStat = await GetPlayerStatsAsync(id, season.Value);
            if (playerStat is null)
                return NotFound();

            var vm = PlayerUtils.GetPlayerDetails(player, playerStat, season.Value, _loungeSettingsService);
            vm.ValidSeasons = _loungeSettingsService.ValidSeasons;
            return View(vm);
        }

        [ResponseCache(Duration = 180, VaryByQueryKeys = new string[] { "*" })]
        [Route("TableDetails/{id}")]
        public async Task<IActionResult> TableDetails(int id)
        {
            var table = await _context.Tables
                .AsNoTracking()
                .SelectPropertiesForTableDetails()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (table is null)
                return NotFound();

            return View(TableUtils.GetTableDetails(table, _loungeSettingsService));
        }

        // since table images are expensive, lets let them be cached for 30 minutes
        [ResponseCache(Duration = 30 * 60, VaryByQueryKeys = new string[] { "*" })]
        [Route("TableImage/{id}.png")]
        public async Task<IActionResult> TableImage(int id)
        {
            var table = await _context.Tables.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

            if (table is null)
                return NotFound();

            var stream = await _tableImageService.DownloadTableImageAsync(id);
            if (stream is null)
            {
                var url = table.Url;
                var tableImage = await TableUtils.GetImageDataAsync(url);
                await _tableImageService.UploadTableImageAsync(id, tableImage);
                return File(tableImage, "image/png");
            }

            return File(stream, "image/png");
        }

        [Route("/error")]
        public IActionResult Error() => Problem();

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
