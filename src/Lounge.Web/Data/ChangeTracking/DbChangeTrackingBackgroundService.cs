using Lounge.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
            _dbCache = dbCache ?? throw new ArgumentNullException(nameof(dbCache));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            long lastSynchronizationVersion = 0;
            while (true)
            {
                try
                {
                    using var scope = _services.CreateScope();

                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var changeTracker = scope.ServiceProvider.GetRequiredService<IChangeTracker>();
                    var synchronizationVersion = await changeTracker.GetCurrentSynchronizationVersionAsync();

                    var bonuses = await context.Bonuses.AsNoTracking().ToListAsync();
                    var penalties = await context.Penalties.AsNoTracking().ToListAsync();
                    var placements = await context.Placements.AsNoTracking().ToListAsync();
                    var players = await context.Players.AsNoTracking().ToListAsync();
                    var playerSeasonData = await context.PlayerSeasonData.AsNoTracking().ToListAsync();

                    var tables = new List<Table>();
                    var maxTableId = (await context.Tables.AnyAsync())
                        ? await context.Tables.Select(t => t.Id).MaxAsync()
                        : 0;

                    for (int i = 0; i < maxTableId; i += 10000)
                    {
                        tables.AddRange(await context.Tables.Where(t => t.Id >= i && t.Id < (i + 10000)).AsNoTracking().ToListAsync());
                        await Task.Delay(10);
                    }

                    var tableScores = new List<TableScore>();
                    for (int i = 0; i < maxTableId; i += 2500)
                    {
                        tableScores.AddRange(await context.TableScores.Where(t => t.TableId >= i && t.TableId < (i + 2500)).AsNoTracking().ToListAsync());
                        await Task.Delay(10);
                    }

                    var nameChanges = await context.NameChanges.AsNoTracking().ToListAsync();

                    _dbCache.Initialize(bonuses, penalties, placements, players, playerSeasonData, tables, tableScores, nameChanges);

                    lastSynchronizationVersion = synchronizationVersion;
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception thrown when initializing DbCache");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
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
                    var nameChanges = await changeTracker.GetNameChangeChangesAsync(lastSynchronizationVersion);

                    if (bonuses.Any() || penalties.Any() || placements.Any() || players.Any() || playerSeasonData.Any() || tables.Any() || tableScores.Any())
                        _dbCache.HandleChanges(bonuses, penalties, placements, players, playerSeasonData, tables, tableScores, nameChanges);

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
