using Lounge.Web.Controllers.ValidationAttributes;
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

        public int Season { get; set; }
        public required IReadOnlySet<string> ValidCountries { get; set; }

        public IActionResult OnGet([ValidSeason] int? season = null)
        {
            // if the season is invalid, just redirect to the default leaderboard
            if (!ModelState.IsValid)
                return RedirectToPage("Leaderboard");

            Season = season ?? _loungeSettingsService.CurrentSeason;
            ValidCountries = _playerStatCache.GetAllCountryCodes(Season);

            return Page();
        }
    }
}