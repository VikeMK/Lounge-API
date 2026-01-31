using Lounge.Web.Models.Enums;
using Lounge.Web.Models.ViewModels;
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
        public bool OtherGameRegistered { get; set; }
        public RegistrationGameMode OtherGame { get; set; }
        public required IReadOnlyList<int> ValidSeasons { get; set; }

        public IActionResult OnGet(string game, int id, int? season = null, int? p=null)
        {
            // Parse the game from route parameter
            if (!Enum.TryParse<RegistrationGameMode>(game, ignoreCase: true, out var parsedGame))
                return NotFound();

            var validSeasons = parsedGame == RegistrationGameMode.mk8dx
                ? _loungeSettingsService.ValidSeasons[GameMode.mk8dx]
                : [.. _loungeSettingsService.ValidSeasons[GameMode.mkworld], .. _loungeSettingsService.ValidSeasons[GameMode.mkworld12p]];

            if (season != null && !validSeasons.Contains(season.Value))
                ModelState.AddModelError(nameof(season), $"Invalid season {season} for game {parsedGame}");

            if (!ModelState.IsValid)
                return RedirectToPage("/PlayerDetails", new { game, id });

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

            var vm = _playerDetailsViewModelService.GetPlayerDetails(id, gameMode, season.Value);
            if (vm is null)
                return NotFound();

            ValidSeasons = validSeasons;
            Data = vm;

            // Calculate other game and registration status
            OtherGame = parsedGame == RegistrationGameMode.mk8dx ? RegistrationGameMode.mkworld : RegistrationGameMode.mk8dx;

            var otherGameMode = OtherGame == RegistrationGameMode.mk8dx ? GameMode.mk8dx : GameMode.mkworld;
            if (!_loungeSettingsService.ValidateCurrentGame(ref otherGameMode, out var otherSeason, out var error, allowMkWorldFallback: true))
                throw new InvalidOperationException("Could not determine other game mode season: " + error);

            var otherGameDetails = _playerDetailsViewModelService.GetPlayerDetails(id, otherGameMode, otherSeason.Value);
            OtherGameRegistered = otherGameDetails != null && otherGameDetails.PlayerId != 0;

            Response.Headers.CacheControl = "public, max-age=180"; // Cache for 5 minutes
            return Page();
        }
    }
}