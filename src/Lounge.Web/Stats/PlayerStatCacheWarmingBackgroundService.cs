using Lounge.Web.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lounge.Web.Stats
{
    public class PlayerStatCacheWarmingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly IPlayerStatCache _cache;

        public PlayerStatCacheWarmingBackgroundService(IServiceProvider services, IPlayerStatCache cache)
        {
            _services = services;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    var playerStatService = scope.ServiceProvider.GetRequiredService<IPlayerStatService>();
                    var stats = await playerStatService.GetAllStatsAsync();
                    _cache.UpdateAllPlayerStats(stats);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
