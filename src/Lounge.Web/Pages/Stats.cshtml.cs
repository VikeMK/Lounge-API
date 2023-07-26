using Lounge.Web.Controllers.ValidationAttributes;
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

        public int Season { get; set; }

        public IActionResult OnGet([ValidSeason] int? season = null)
        {
            // if the season is invalid, just redirect to the default stat page
            if (!ModelState.IsValid)
                return RedirectToPage("Stats");

            Season = season ?? _loungeSettingsService.CurrentSeason;

            return Page();
        }
    }
}