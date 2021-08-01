using Lounge.Web.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lounge.Web.Stats
{
    public class PlayerStatCacheWarmingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly IPlayerStatCache _cache;
        private readonly ILogger<PlayerStatCacheWarmingBackgroundService> _logger;
        private readonly IOptionsMonitor<LoungeSettings> options;
        private readonly Dictionary<int, DateTime> _lastRefreshTimes = new();

        public PlayerStatCacheWarmingBackgroundService(
            IServiceProvider services,
            IPlayerStatCache cache,
            ILogger<PlayerStatCacheWarmingBackgroundService> logger,
            IOptionsMonitor<LoungeSettings> options)
        {
            _services = services;
            _cache = cache;
            _logger = logger;
            this.options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var refreshDelays = options.CurrentValue.LeaderboardRefreshDelaysBySeason;
                
                foreach ((string seasonStr, string delayStr) in refreshDelays)
                {
                    if (!int.TryParse(seasonStr, out var season))
                        continue;

                    if (!TimeSpan.TryParse(delayStr, out var delay))
                        continue;

                    if (!_lastRefreshTimes.TryGetValue(season, out var lastRefresh) || (lastRefresh + delay) < DateTime.UtcNow)
                    {
                        try
                        {
                            using var scope = _services.CreateScope();
                            var playerStatService = scope.ServiceProvider.GetRequiredService<IPlayerStatService>();
                            var stats = await playerStatService.GetAllStatsAsync(season);
                            _cache.UpdateAllPlayerStats(stats, season);
                            _lastRefreshTimes[season] = DateTime.UtcNow;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Exception thrown when updating player stats");
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
