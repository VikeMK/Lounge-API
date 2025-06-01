using Lounge.Web.Controllers.ValidationAttributes;
using Lounge.Web.Models.Enums;
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
        public required PlayerDetailsViewModel Data { get; set; }

        public IActionResult OnGet(int id, Game game = Game.MK8DX, [ValidSeason] int? season = null)
        {
            if (!ModelState.IsValid)
                return RedirectToPage("/PlayerDetails", new { id });

            var vm = _playerDetailsViewModelService.GetPlayerDetails(id, game, season ?? _loungeSettingsService.CurrentSeason[game]);
            if (vm is null)
                return NotFound();

            vm.ValidSeasons = _loungeSettingsService.ValidSeasons[game];

            Data = vm;
            return Page();
        }
    }
}