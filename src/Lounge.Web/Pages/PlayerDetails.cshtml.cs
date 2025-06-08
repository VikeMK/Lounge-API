using Lounge.Web.Controllers.ValidationAttributes;
using Lounge.Web.Models.Enums;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Settings;
using Lounge.Web.Stats;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

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
        
        public IActionResult OnGet(string game, int id, [ValidSeason] int? season = null)
        {
            // Parse the game from route parameter
            if (!Enum.TryParse<Game>(game, ignoreCase: true, out var parsedGame))
                return NotFound();

            if (!ModelState.IsValid)
                return RedirectToPage("/PlayerDetails", new { game, id });

            var vm = _playerDetailsViewModelService.GetPlayerDetails(id, parsedGame, season ?? _loungeSettingsService.CurrentSeason[parsedGame]);
            if (vm is null)
                return NotFound();

            vm.ValidSeasons = _loungeSettingsService.ValidSeasons[parsedGame];

            Data = vm;
            return Page();
        }
    }
}