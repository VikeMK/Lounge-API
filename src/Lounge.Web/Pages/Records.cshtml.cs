using Lounge.Web.Controllers.ValidationAttributes;
using Lounge.Web.Settings;
using Lounge.Web.Stats;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

        public int Season { get; set; }
        public required RecordsCache.SeasonRecords Records { get; set; }

        public IActionResult OnGet([ValidSeason] int? season = null)
        {
            // if the season is invalid, just redirect to the default records page
            if (!ModelState.IsValid)
                return RedirectToPage("Records");

            Season = season ?? _loungeSettingsService.CurrentSeason;
            Records = _recordsCache.GetRecords(Season);

            return Page();
        }
    }
}