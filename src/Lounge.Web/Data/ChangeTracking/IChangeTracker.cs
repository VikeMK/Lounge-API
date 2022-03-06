using Lounge.Web.Data.Entities.ChangeTracking;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lounge.Web.Data.ChangeTracking
{
    public interface IChangeTracker
    {
        Task<long> GetCurrentSynchronizationVersionAsync();
        Task<List<BonusChange>> GetBonusChangesAsync(long lastSynchronizationVersion);
        Task<List<PenaltyChange>> GetPenaltyChangesAsync(long lastSynchronizationVersion);
        Task<List<PlacementChange>> GetPlacementChangesAsync(long lastSynchronizationVersion);
        Task<List<PlayerChange>> GetPlayerChangesAsync(long lastSynchronizationVersion);
        Task<List<PlayerSeasonDataChange>> GetPlayerSeasonDataChangesAsync(long lastSynchronizationVersion);
        Task<List<TableChange>> GetTableChangesAsync(long lastSynchronizationVersion);
        Task<List<TableScoreChange>> GetTableScoreChangesAsync(long lastSynchronizationVersion);
        Task<List<NameChangeChange>> GetNameChangeChangesAsync(long lastSynchronizationVersion);
    }
}