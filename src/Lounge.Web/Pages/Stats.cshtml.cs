using Lounge.Web.Controllers.ValidationAttributes;
using Lounge.Web.Models.Enums;
using Lounge.Web.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace Lounge.Web.Pages
{
    public class StatsPageModel : PageModel
    {
        private readonly ILoungeSettingsService _loungeSettingsService;


        public StatsPageModel(ILoungeSettingsService loungeSettingsService)
        {
            _loungeSettingsService = loungeSettingsService;
        }

        public Game Game { get; set; }
        public int Season { get; set; }        public IActionResult OnGet(string game, [ValidSeason] int? season = null)
        {
            // Parse the game from route parameter
            if (!Enum.TryParse<Game>(game, ignoreCase: true, out var parsedGame))
                return NotFound();

            // if the season is invalid, just redirect to the default stat page
            if (!ModelState.IsValid)
                return RedirectToPage("Stats", new { game });

            Game = parsedGame;
            Season = season ?? _loungeSettingsService.CurrentSeason[parsedGame];

            return Page();
        }
    }
}