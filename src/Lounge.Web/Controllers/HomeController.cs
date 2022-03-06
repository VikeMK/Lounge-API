using Lounge.Web.Data;
using Lounge.Web.Storage;
using Lounge.Web.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Lounge.Web.Controllers
{
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITableImageService _tableImageService;

        public HomeController(ApplicationDbContext context, ITableImageService tableImageService)
        {
            _context = context;
            _tableImageService = tableImageService;
        }

        // since table images are expensive, lets let them be cached for 30 minutes
        [ResponseCache(Duration = 30 * 60)]
        [Route("TableImage/{id}.png")]
        public async Task<IActionResult> TableImage(int id)
        {
            var table = await _context.Tables.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

            if (table is null)
                return NotFound();

            try
            {
                var stream = await _tableImageService.DownloadTableImageAsync(id);
                if (stream is not null)
                {
                    return File(stream, "image/png");
                }
            }
            catch
            {
                // swallow exception
            }

            var tableScores = await _context.TableScores
                .Where(s => s.TableId == id)
                .Select(s => new { s.Team, s.Score, s.Player.Name, s.Player.CountryCode })
                .ToListAsync();

            var scores = new (string Player, string? CountryCode, int Score)[table.NumTeams][];
            for (int i = 0; i < table.NumTeams; i++)
            {
                scores[i] = tableScores
                    .Where(score => score.Team == i)
                    .Select(score => (score.Name, score.CountryCode, score.Score))
                    .ToArray();
            }

            var url = TableUtils.BuildUrl(table.Tier, scores);
            var tableImage = await TableUtils.GetImageDataAsync(url);
            try
            {
                await _tableImageService.UploadTableImageAsync(id, tableImage);
            }
            catch
            {
                // swallow exception
            }

            return File(tableImage, "image/png");
        }
    }
}
