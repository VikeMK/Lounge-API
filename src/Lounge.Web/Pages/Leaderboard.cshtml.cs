using Lounge.Web.Controllers.ValidationAttributes;
using Lounge.Web.Models.Enums;
using Lounge.Web.Settings;
using Lounge.Web.Stats;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

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

        public Game Game { get; set; }
        public int Season { get; set; }
        public required IReadOnlySet<string> ValidCountries { get; set; }

        public IActionResult OnGet(Game game = Game.MK8DX, [ValidSeason] int? season = null)
        {
            // if the season is invalid, just redirect to the default leaderboard
            if (!ModelState.IsValid)
                return RedirectToPage("Leaderboard");

            Game = game;
            Season = season ?? _loungeSettingsService.CurrentSeason[game];
            ValidCountries = _playerStatCache.GetAllCountryCodes(game, Season);

            return Page();
        }
    }
}