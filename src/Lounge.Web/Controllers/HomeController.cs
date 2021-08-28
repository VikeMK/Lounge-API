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
        private readonly IPlayerDetailsViewModelService _playerDetailsViewModelService;
        private readonly ITableImageService _tableImageService;
        private readonly ILoungeSettingsService _loungeSettingsService;

        public HomeController(
            ApplicationDbContext context, 
            IPlayerDetailsViewModelService playerDetailsViewModelService,
            IPlayerStatCache playerStatCache,
            ITableImageService tableImageService,
            ILoungeSettingsService loungeSettingsService)
        {
            _context = context;
            _playerDetailsViewModelService = playerDetailsViewModelService;
            _playerStatCache = playerStatCache;
            _tableImageService = tableImageService;
            _loungeSettingsService = loungeSettingsService;
        }

        [ResponseCache(Duration = 180)]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Leaderboard));
        }

        [ResponseCache(Duration = 180, VaryByQueryKeys = new string[] { "season" })]
        [Route("Leaderboard")]
        public ActionResult<LeaderboardPageViewModel> Leaderboard([ValidSeason] int? season = null)
        {
            // if the season is invalid, just redirect to the default leaderboard
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Leaderboard));

            season ??= _loungeSettingsService.CurrentSeason;
            var validCountries = _playerStatCache.GetAllCountryCodes(season.Value);

            return View(new LeaderboardPageViewModel(season.Value, validCountries));
        }

        [ResponseCache(Duration = 180, VaryByQueryKeys = new string[] { "season" })]
        [Route("PlayerDetails/{id}")]
        public IActionResult PlayerDetails(int id, [ValidSeason] int? season=null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            season ??= _loungeSettingsService.CurrentSeason;

            var vm = _playerDetailsViewModelService.GetPlayerDetails(id, season.Value);
            if (vm is null)
                return NotFound();

            vm.ValidSeasons = _loungeSettingsService.ValidSeasons;
            return View(vm);
        }

        [ResponseCache(Duration = 180)]
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
        [ResponseCache(Duration = 30 * 60)]
        [Route("TableImage/{id}.png")]
        public async Task<IActionResult> TableImage(int id)
        {
            var table = await _context.Tables.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

            if (table is null)
                return NotFound();

            var stream = await _tableImageService.DownloadTableImageAsync(id);
            if (stream is null)
            {
                var tableScores = await _context.TableScores
                    .Where(s => s.TableId == id)
                    .Select(s => new { s.Team, s.Score, s.Player.Name, s.Player.CountryCode })
                    .ToListAsync();

                var scores = new (string Player, string? CountryCode, int Score)[table.NumTeams][];
                for (int i = 0; i < table.NumTeams; i++)
                {
                    scores[i] = tableScores
                        .Where(score => score.Team == i)
                        .Select(score => (score.Name, score.CountryCode, score.Score))
                        .ToArray();
                }

                var url = TableUtils.BuildUrl(table.Tier, scores);
                var tableImage = await TableUtils.GetImageDataAsync(url);
                await _tableImageService.UploadTableImageAsync(id, tableImage);
                return File(tableImage, "image/png");
            }

            return File(stream, "image/png");
        }

        [Route("/error")]
        public IActionResult Error() => Problem();
    }
}
