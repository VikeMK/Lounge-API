using Lounge.Web.Data;
using Lounge.Web.Models;
using Microsoft.EntityFrameworkCore;
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
            var players = await _context.Players
                .Where(p => p.RegistryId == null)
                .ToListAsync();

            foreach (var player in players)
            {
                var registryId = await _api.GetRegistryIdAsync(player.MKCId);
                if (registryId != null)
                {
                    player.RegistryId = registryId;
                }

                await Task.Delay(50);
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
                    player.CountryCode = countryCode is not ("ZZ" or "XX") ? countryCode : null;
                    player.SwitchFc = registryPlayerData.SwitchFc;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
