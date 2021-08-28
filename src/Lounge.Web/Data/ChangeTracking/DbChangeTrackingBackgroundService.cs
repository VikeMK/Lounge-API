using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lounge.Web.Data.ChangeTracking
{
    public class DbChangeTrackingBackgroundService : BackgroundService
    {
        private readonly ILogger<DbChangeTrackingBackgroundService> _logger;
        private readonly IServiceProvider _services;
        private readonly IChangeTrackingSubscriber _dbCache;

        public DbChangeTrackingBackgroundService(
            ILogger<DbChangeTrackingBackgroundService> logger,
            IServiceProvider services,
            IChangeTrackingSubscriber dbCache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _dbCache = dbCache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            long lastSynchronizationVersion = 0;
            using (var scope = _services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var changeTracker = scope.ServiceProvider.GetRequiredService<IChangeTracker>();
                lastSynchronizationVersion = await changeTracker.GetCurrentSynchronizationVersionAsync();

                var bonuses = await context.Bonuses.AsNoTracking().ToListAsync();
                var penalties = await context.Penalties.AsNoTracking().ToListAsync();
                var placements = await context.Placements.AsNoTracking().ToListAsync();
                var players = await context.Players.AsNoTracking().ToListAsync();
                var playerSeasonData = await context.PlayerSeasonData.AsNoTracking().ToListAsync();
                var tables = await context.Tables.AsNoTracking().ToListAsync();
                var tableScores = await context.TableScores.AsNoTracking().ToListAsync();

                _dbCache.Initialize(bonuses, penalties, placements, players, playerSeasonData, tables, tableScores);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var changeTracker = scope.ServiceProvider.GetRequiredService<IChangeTracker>();

                    var synchronizationVersion = await changeTracker.GetCurrentSynchronizationVersionAsync();

                    var bonuses = await changeTracker.GetBonusChangesAsync(lastSynchronizationVersion);
                    var penalties = await changeTracker.GetPenaltyChangesAsync(lastSynchronizationVersion);
                    var placements = await changeTracker.GetPlacementChangesAsync(lastSynchronizationVersion);
                    var players = await changeTracker.GetPlayerChangesAsync(lastSynchronizationVersion);
                    var playerSeasonData = await changeTracker.GetPlayerSeasonDataChangesAsync(lastSynchronizationVersion);
                    var tables = await changeTracker.GetTableChangesAsync(lastSynchronizationVersion);
                    var tableScores = await changeTracker.GetTableScoreChangesAsync(lastSynchronizationVersion);

                    if (bonuses.Any() || penalties.Any() || placements.Any() || players.Any() || playerSeasonData.Any() || tables.Any() || tableScores.Any())
                        _dbCache.HandleChanges(bonuses, penalties, placements, players, playerSeasonData, tables, tableScores);

                    lastSynchronizationVersion = synchronizationVersion;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception thrown when passing changes to subscribers");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
