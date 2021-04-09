using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lounge.Web.Stats
{
    public interface IPlayerStatService
    {
        public Task<PlayerStat?> GetPlayerStatsByIdAsync(int id);

        public Task<IReadOnlyList<PlayerStat>> GetAllStatsAsync();
    }
}
