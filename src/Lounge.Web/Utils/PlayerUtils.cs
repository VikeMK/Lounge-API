using Lounge.Web.Data.Entities;
using Lounge.Web.Models.ViewModels;
using System;

namespace Lounge.Web.Utils
{
    public static class PlayerUtils
    {
        public static string NormalizeName(string name) => string.Join("", name.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

        public static PlayerViewModel GetPlayerViewModel(Player player, PlayerSeasonData? seasonData)
        {
            return new PlayerViewModel
            {
                Id = player.Id,
                DiscordId = player.DiscordId,
                MKCId = player.MKCId,
                Name = player.Name,
                CountryCode = player.CountryCode,
                SwitchFc = player.SwitchFc,
                IsHidden = player.IsHidden,
                Mmr = seasonData?.Mmr,
                MaxMmr = seasonData?.MaxMmr,
            };
        }
    }
}
