using Lounge.Web.Models.Enums;
using Lounge.Web.Settings;
using Lounge.Web.Stats;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;

namespace Lounge.Web.Pages
{
    public class RecordsPageModel : PageModel
    {
        private readonly IRecordsCache _recordsCache;
        private readonly ILoungeSettingsService _loungeSettingsService;

        public RecordsPageModel(IRecordsCache recordsCache, ILoungeSettingsService loungeSettingsService)
        {
            _recordsCache = recordsCache;
            _loungeSettingsService = loungeSettingsService;
        }

        public Game Game { get; set; }
        public int Season { get; set; }
        public required RecordsCache.SeasonRecords Records { get; set; }
        
        public IActionResult OnGet(string game, int? season = null)
        {
            // Parse the game from route parameter
            if (!Enum.TryParse<Game>(game, ignoreCase: true, out var parsedGame))
                return NotFound();

            if (season != null && !_loungeSettingsService.ValidSeasons[parsedGame].Contains(season.Value))
                ModelState.AddModelError(nameof(season), $"Invalid season {season} for game {parsedGame}");

            // if the season is invalid, just redirect to the default records page
            if (!ModelState.IsValid)
                return RedirectToPage("Records", new { game });

            Game = parsedGame;
            Season = season ?? _loungeSettingsService.CurrentSeason[parsedGame];
            Records = _recordsCache.GetRecords(parsedGame, Season);

            return Page();
        }
    }
}