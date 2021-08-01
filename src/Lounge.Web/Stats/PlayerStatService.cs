using Lounge.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lounge.Web.Stats
{
    public class PlayerStatService : IPlayerStatService
    {
        private readonly ApplicationDbContext _context;

        public PlayerStatService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<PlayerStat>> GetAllStatsAsync(int season)
        {
            return await _context.Players
                .AsNoTracking()
                .SelectPlayerStats(season)
                .ToListAsync();
        }

        public async Task<PlayerStat?> GetPlayerStatsByIdAsync(int id, int season)
        {
            return await _context.Players
                .AsNoTracking()
                .SelectPlayerStats(season)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
    }
}
