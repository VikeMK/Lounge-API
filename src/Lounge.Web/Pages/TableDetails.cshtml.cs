using Lounge.Web.Data;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Settings;
using Lounge.Web.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Lounge.Web.Pages
{
    public class TableDetailsPageModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILoungeSettingsService _loungeSettingsService;

        public TableDetailsPageModel(ApplicationDbContext context, ILoungeSettingsService loungeSettingsService)
        {
            _context = context;
            _loungeSettingsService = loungeSettingsService;
        }

        public required TableDetailsViewModel Data { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var table = await _context.Tables
                .AsNoTracking()
                .SelectPropertiesForTableDetails()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (table is null)
                return NotFound();

            Data = TableUtils.GetTableDetails(table, _loungeSettingsService);

            return Page();
        }
    }
}

