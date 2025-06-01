using Lounge.Web.Controllers.ValidationAttributes;
using Lounge.Web.Models.Enums;
using Lounge.Web.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
        public int Season { get; set; }

        public IActionResult OnGet(Game game = Game.mk8dx, [ValidSeason] int? season = null)
        {
            // if the season is invalid, just redirect to the default stat page
            if (!ModelState.IsValid)
                return RedirectToPage("Stats");

            Game = game;
            Season = season ?? _loungeSettingsService.CurrentSeason[game];

            return Page();
        }
    }
}