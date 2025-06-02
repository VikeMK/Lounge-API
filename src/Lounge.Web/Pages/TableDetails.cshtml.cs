using Lounge.Web.Data;
using Lounge.Web.Models.Enums;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Settings;
using Lounge.Web.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
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

        public required TableDetailsViewModel Data { get; set; }        public async Task<IActionResult> OnGetAsync(string game, int id)
        {
            // Parse the game from route parameter
            if (!Enum.TryParse<Game>(game, ignoreCase: true, out var parsedGame))
                return NotFound();

            var table = await _context.Tables
                .AsNoTracking()
                .SelectPropertiesForTableDetails()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (table is null)
                return NotFound();

            // Validate that the table belongs to the specified game
            if ((Game)table.Game != parsedGame)
                return NotFound();

            Data = TableUtils.GetTableDetails(table, _loungeSettingsService);

            return Page();
        }
    }
}

