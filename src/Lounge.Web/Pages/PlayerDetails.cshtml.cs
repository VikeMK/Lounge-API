using Lounge.Web.Controllers.ValidationAttributes;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Settings;
using Lounge.Web.Stats;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lounge.Web.Pages
{
    public class PlayerDetailsPageModel : PageModel
    {
        private readonly ILoungeSettingsService _loungeSettingsService;
        private readonly IPlayerDetailsViewModelService _playerDetailsViewModelService;

        public PlayerDetailsPageModel(ILoungeSettingsService loungeSettingsService, IPlayerDetailsViewModelService playerDetailsViewModelService)
        {
            _loungeSettingsService = loungeSettingsService;
            _playerDetailsViewModelService = playerDetailsViewModelService;
        }
        public PlayerDetailsViewModel Data { get; set; }

        public IActionResult OnGet(int id, [ValidSeason] int? season = null)
        {
            if (!ModelState.IsValid)
                return RedirectToPage("/PlayerDetails", new { id });

            var vm = _playerDetailsViewModelService.GetPlayerDetails(id, season ?? _loungeSettingsService.CurrentSeason);
            if (vm is null)
                return NotFound();

            vm.ValidSeasons = _loungeSettingsService.ValidSeasons;

            Data = vm;
            return Page();
        }
    }
}