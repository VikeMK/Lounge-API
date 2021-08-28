using Lounge.Web.Models;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Settings;
using Lounge.Web.Stats;
using System;
using System.Collections.Generic;
using System.Linq;

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
