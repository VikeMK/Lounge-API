using Lounge.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public async Task<IReadOnlyList<PlayerStat>> GetAllStatsAsync()
        {
            return await _context.Players
                .AsNoTracking()
                .SelectPlayerStats()
                .ToListAsync();
        }

        public async Task<PlayerStat?> GetPlayerStatsByIdAsync(int id)
        {
            return await _context.Players
                .AsNoTracking()
                .SelectPlayerStats()
                .FirstOrDefaultAsync(s => s.Id == id);
        }
    }
}
