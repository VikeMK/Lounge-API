using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lounge.Web.Stats
{
    public class PlayerStatCacheWarmingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly IPlayerStatCache _cache;
        private readonly ILogger<PlayerStatCacheWarmingBackgroundService> _logger;

        public PlayerStatCacheWarmingBackgroundService(IServiceProvider services, IPlayerStatCache cache, ILogger<PlayerStatCacheWarmingBackgroundService> logger)
        {
            _services = services;
            _cache = cache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var playerStatService = scope.ServiceProvider.GetRequiredService<IPlayerStatService>();
                        var stats = await playerStatService.GetAllStatsAsync();
                        _cache.UpdateAllPlayerStats(stats);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception thrown when updating player stats");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
