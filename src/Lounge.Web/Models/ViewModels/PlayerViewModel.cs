using Lounge.Web.Data.Entities;
using Lounge.Web.Models.Enums;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lounge.Web.Models.ViewModels
{
    public class PlayerViewModel
    {
        [SetsRequiredMembers]
        public PlayerViewModel(Player player)
        {
            Id = player.Id;
            DiscordId = player.DiscordId;
            MKCId = player.RegistryId ?? -1;
            RegistryId = player.RegistryId;
            Name = player.Name;
            CountryCode = player.CountryCode;
            SwitchFc = player.SwitchFc;
            IsHidden = player.IsHidden;
        }

        public required int Id { get; init; }
        public required string Name { get; init; }
        public required int MKCId { get; init; }
        public required int? RegistryId { get; init; }
        public required string? DiscordId { get; init; }
        public required string? CountryCode { get; init; }
        public required string? SwitchFc { get; init; }
        public required bool IsHidden { get; init; }
    }

    public class PlayerGameViewModel : PlayerViewModel
    {
        [SetsRequiredMembers]
        public PlayerGameViewModel(Player player, GameMode game, int season, PlayerSeasonData? seasonData) : base(player)
        {
            Game = game;
            Season = season;
            Mmr = seasonData?.Mmr;
            MaxMmr = seasonData?.MaxMmr;
        }

        public required GameMode Game { get; init; }
        public required int Season { get; init; }
        public required int? Mmr { get; init; }
        public required int? MaxMmr { get; init; }
    }

    public class PlayerAllGamesViewModel : PlayerViewModel
    {
        [SetsRequiredMembers]
        public PlayerAllGamesViewModel(Player player, List<string> registrations) : base(player)
        {
            Registrations = registrations;
        }

        public required List<string> Registrations { get; init; }
    }
}
