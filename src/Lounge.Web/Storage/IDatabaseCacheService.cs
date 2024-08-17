using Lounge.Web.Data.ChangeTracking;
using System.Threading.Tasks;

namespace Lounge.Web.Storage
{
    public interface IDatabaseCacheService
    {
        Task<DatabaseCacheService.DatabaseCache?> GetLatestCacheDataAsync();
        Task UpdateLatestCacheDataAsync(long version, DbCacheData dbCache);
    }
}