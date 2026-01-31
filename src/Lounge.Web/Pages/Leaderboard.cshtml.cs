using Lounge.Web.Models.Enums;
using Lounge.Web.Settings;
using Lounge.Web.Stats;
using Lounge.Web.Utils;
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
        public GameMode Game { get; set; }
        public int Season { get; set; }
        public bool IsCurrentSeason { get; set; }
        public required IReadOnlyList<int> ValidSeasons { get; set; }
        public required IReadOnlySet<string> ValidCountries { get; set; }
        public bool ShowExtra { get; set; }
        
        public IActionResult OnGet(string game, int? season = null, int? extra = null, int? p=null)
        {
            // Parse the game from route parameter
            if (!Enum.TryParse<RegistrationGameMode>(game, ignoreCase: true, out var parsedGame))
                return NotFound();

            ValidSeasons = parsedGame == RegistrationGameMode.mk8dx
                ? _loungeSettingsService.ValidSeasons[GameMode.mk8dx]
                : [.. _loungeSettingsService.ValidSeasons[GameMode.mkworld], .. _loungeSettingsService.ValidSeasons[GameMode.mkworld12p]];

            if (season != null && !ValidSeasons.Contains(season.Value))
                ModelState.AddModelError(nameof(season), $"Invalid season {season} for game {parsedGame}");

            // if the season is invalid, just redirect to the default leaderboard
            if (!ModelState.IsValid)
                return RedirectToPage("Leaderboard", new { game });

            var gameMode = parsedGame switch
            {
                RegistrationGameMode.mkworld => season switch
                {
                    0 or 1 => GameMode.mkworld,
                    _ => p == 12 ? GameMode.mkworld12p : GameMode.mkworld24p,
                },
                RegistrationGameMode.mk8dx => GameMode.mk8dx,
                _ => throw new ArgumentOutOfRangeException(nameof(game)),
            };

            if (season == null)
            {
                if (!_loungeSettingsService.TryGetCurrentSeason(gameMode, out season))
                    throw new InvalidOperationException("Could not determine current season for game mode " + gameMode);
            }

            Game = gameMode;
            Season = season.Value;
            IsCurrentSeason = Season == season.Value;
            ValidCountries = _playerStatCache.GetAllCountryCodes(gameMode, Season);
            CachesBeingBuilt = ValidCountries.Count == 0; // If no countries, assume caches are being built
            ShowExtra = extra == 1;

            return Page();
        }
    }
}