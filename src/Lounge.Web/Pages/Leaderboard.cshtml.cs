using Lounge.Web.Models.Enums;
using Lounge.Web.Settings;
using Lounge.Web.Stats;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lounge.Web.Pages
{
    public class LeaderboardPageModel : PageModel
    {
        private readonly ILoungeSettingsService _loungeSettingsService;
        private readonly IPlayerStatCache _playerStatCache;

        public LeaderboardPageModel(ILoungeSettingsService loungeSettingsService, IPlayerStatCache playerStatCache)
        {
            _loungeSettingsService = loungeSettingsService;
            _playerStatCache = playerStatCache;
        }

        public bool CachesBeingBuilt { get; set; }
        public Game Game { get; set; }
        public int Season { get; set; }
        public bool IsCurrentSeason { get; set; }
        public required IReadOnlySet<string> ValidCountries { get; set; }
        public bool ShowExtra { get; set; }
        
        public IActionResult OnGet(string game, int? season = null, int? extra = null)
        {
            // Parse the game from route parameter
            if (!Enum.TryParse<Game>(game, ignoreCase: true, out var parsedGame))
                return NotFound();

            if (season != null && !_loungeSettingsService.ValidSeasons[parsedGame].Contains(season.Value))
                ModelState.AddModelError(nameof(season), $"Invalid season {season} for game {parsedGame}");

            // if the season is invalid, just redirect to the default leaderboard
            if (!ModelState.IsValid)
                return RedirectToPage("Leaderboard", new { game });

            Game = parsedGame;
            Season = season ?? _loungeSettingsService.CurrentSeason[parsedGame];
            IsCurrentSeason = Season == _loungeSettingsService.CurrentSeason[parsedGame];
            ValidCountries = _playerStatCache.GetAllCountryCodes(parsedGame, Season);
            CachesBeingBuilt = ValidCountries.Count == 0; // If no countries, assume caches are being built
            ShowExtra = extra == 1;

            return Page();
        }
    }
}