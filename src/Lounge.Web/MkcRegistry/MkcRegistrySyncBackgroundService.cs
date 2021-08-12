using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lounge.Web.Stats
{
    public class MkcRegistrySyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<MkcRegistrySyncBackgroundService> _logger;

        public MkcRegistrySyncBackgroundService(IServiceProvider services, ILogger<MkcRegistrySyncBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var registryDataUpdater = scope.ServiceProvider.GetRequiredService<IMkcRegistryDataUpdater>();
                    await registryDataUpdater.UpdateRegistryDataAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception thrown when updating registry data");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
