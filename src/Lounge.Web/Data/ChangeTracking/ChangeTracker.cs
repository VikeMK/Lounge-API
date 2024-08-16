using Lounge.Web.Data.Entities;
using Lounge.Web.Data.Entities.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lounge.Web.Data.ChangeTracking
{
    public class ChangeTracker : IChangeTracker
    {
        private readonly ApplicationDbContext _context;

        public ChangeTracker(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<long> GetCurrentSynchronizationVersionAsync() =>
            (await _context.Set<ChangeTrackingCurrentVersion>()
                .FromSqlRaw("SELECT CHANGE_TRACKING_CURRENT_VERSION() as CurrentVersion")
                .FirstAsync()).CurrentVersion;

        public async Task<List<BonusChange>> GetBonusChangesAsync(long lastSynchronizationVersion) =>
            await GetTableChangesAsync<BonusChange, Bonus>("Bonuses", lastSynchronizationVersion);

        public async Task<List<PenaltyChange>> GetPenaltyChangesAsync(long lastSynchronizationVersion) =>
            await GetTableChangesAsync<PenaltyChange, Penalty>("Penalties", lastSynchronizationVersion);

        public async Task<List<PlacementChange>> GetPlacementChangesAsync(long lastSynchronizationVersion) =>
            await GetTableChangesAsync<PlacementChange, Placement>("Placements", lastSynchronizationVersion);

        public async Task<List<PlayerChange>> GetPlayerChangesAsync(long lastSynchronizationVersion) =>
            await GetTableChangesAsync<PlayerChange, Player>("Players", lastSynchronizationVersion);

        public async Task<List<PlayerSeasonDataChange>> GetPlayerSeasonDataChangesAsync(long lastSynchronizationVersion) =>
            await GetTableChangesAsync<PlayerSeasonDataChange, PlayerSeasonData>("PlayerSeasonData", lastSynchronizationVersion);

        public async Task<List<TableChange>> GetTableChangesAsync(long lastSynchronizationVersion) =>
            await GetTableChangesAsync<TableChange, Table>("Tables", lastSynchronizationVersion);

        public async Task<List<TableScoreChange>> GetTableScoreChangesAsync(long lastSynchronizationVersion) =>
            await GetTableChangesAsync<TableScoreChange, TableScore>("TableScores", lastSynchronizationVersion);

        public async Task<List<NameChangeChange>> GetNameChangeChangesAsync(long lastSynchronizationVersion) =>
            await GetTableChangesAsync<NameChangeChange, NameChange>("NameChanges", lastSynchronizationVersion);

        private async Task<List<TChange>> GetTableChangesAsync<TChange, TEntity>(string tableName, long lastSynchronizationVersion)
            where TChange : Change<TEntity>
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
            where TEntity : class =>
            await _context.Set<TChange>()
                .FromSqlRaw($"SELECT * FROM CHANGETABLE(CHANGES dbo.{tableName}, {{0}}) AS CT", lastSynchronizationVersion)
                .Include(x => x.Entity)
                .ToListAsync();
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.
    }
}
