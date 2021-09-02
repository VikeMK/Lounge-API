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

        public async Task UpdateRegistryIdsAsync()
        {
            async Task UpdateRegistryIdForPlayerAsync(Player player)
            {
                var registryId = await _api.GetRegistryIdAsync(player.MKCId);
                if (registryId != null)
                    player.RegistryId = registryId;
            }

            var players = await _context.Players
                .Where(p => p.RegistryId == null)
                .ToListAsync();

            const int batchSize = 10;
            for (int i = 0; i < players.Count; i += batchSize)
            {
                var tasks = new List<Task>();
                for (int j = i; j < Math.Min(i + batchSize, players.Count); j++)
                {
                    tasks.Add(UpdateRegistryIdForPlayerAsync(players[j]));
                }

                await Task.WhenAll(tasks);
                await Task.Delay(500);
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateRegistryDataAsync()
        {
            var registryData = await _api.GetAllPlayersRegistryDataAsync();
            var registryDataLookup = registryData.ToDictionary(r => r.RegistryId);

            var players = await _context.Players
                .Where(p => p.RegistryId != null)
                .ToListAsync();

            foreach (var player in players)
            {
                if (registryDataLookup.TryGetValue(player.RegistryId!.Value, out var registryPlayerData))
                {
                    var countryCode = registryPlayerData.CountryCode;

                    // ZZ and XX are invalid country codes that should be treated as null
                    if (countryCode is "ZZ" or "XX")
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
