using Lounge.Web.Data;
using Lounge.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lounge.Web.Stats
{
    public class MkcRegistryDataUpdater : IMkcRegistryDataUpdater
    {
        private readonly ApplicationDbContext _context;
        private readonly IMkcRegistryApi _api;

        public MkcRegistryDataUpdater(ApplicationDbContext context, IMkcRegistryApi api)
        {
            _context = context;
            _api = api;
        }

        public async Task UpdateRegistryDataAsync()
        {
            var registryData = await _api.GetAllPlayersRegistryDataAsync();
            var registryDataLookup = registryData.ToDictionary(r => r.ForumId);

            var players = await _context.Players.ToListAsync();

            foreach (var player in players)
            {
                if (registryDataLookup.TryGetValue(player.MKCId, out var registryPlayerData))
                {
                    if (player.RegistryId != registryPlayerData.RegistryId)
                        player.RegistryId = registryPlayerData.RegistryId;

                    var countryCode = registryPlayerData.CountryCode;

                    // ZZ and XX are invalid country codes that should be treated as null
                    // AQ is Antarctica and is used by individuals who want their country to be hidden
                    if (countryCode is "ZZ" or "XX" or "AQ")
                        countryCode = null;

                    if (player.CountryCode != countryCode)
                        player.CountryCode = countryCode;

                    if (player.SwitchFc != registryPlayerData.SwitchFc)
                        player.SwitchFc = registryPlayerData.SwitchFc;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
