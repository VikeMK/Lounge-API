using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.AspNetCore.Authorization;
using Lounge.Web.Data;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Lounge.Web.Utils;
using Lounge.Web.Settings;
using System.Linq;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Data.Entities;
using Lounge.Web.Models.Enums;

namespace Lounge.Web.Controllers
{
    [Route("api/penaltyrequest")]
    [Authorize]
    [ApiController]
    public class PenaltyRequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILoungeSettingsService _loungeSettingsService;

        public PenaltyRequestsController(ApplicationDbContext context, ILoungeSettingsService loungeSettingsService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _loungeSettingsService = loungeSettingsService;
        }

        [HttpGet]
        public async Task<ActionResult<PenaltyRequestViewModel>> GetPenaltyRequest(int id, Game game = Game.mk8dx)
        {
            var requestData = await _context.PenaltyRequests
                .AsNoTracking()
                .Where(r => r.Id == id && r.Game == (int)game)
                .Select(r => new { PenaltyRequest = r, PlayerName = r.Player.Name, ReporterName = r.Reporter == null ? string.Empty : r.Reporter.Name })
                .FirstOrDefaultAsync();

            if (requestData is null)
            {
                return NotFound();
            }

            return PenaltyRequestUtils.GetPenaltyRequestDetails(requestData.PenaltyRequest, requestData.PlayerName, requestData.ReporterName);
        }

        [HttpGet("list")]
        public async Task<ActionResult<List<PenaltyRequestViewModel>>> GetPenaltyRequests(Game game = Game.mk8dx)
        {
            var requestsData = await _context.PenaltyRequests
                .AsNoTracking()
                .Where(r => r.Game == (int)game)
                .Select(r => new { PenaltyRequest = r, PlayerName = r.Player.Name, ReporterName = r.Reporter == null ? string.Empty : r.Reporter.Name})
                .ToArrayAsync();

            if (requestsData is null)
            {
                return NotFound();
            }

            return requestsData.Select(r => PenaltyRequestUtils.GetPenaltyRequestDetails(r.PenaltyRequest, r.PlayerName, r.ReporterName)).ToList();
        }

        [HttpPost("create")]
        public async Task<ActionResult<PenaltyRequestViewModel>> SubmitPenaltyRequest(string penaltyType, string playerName, string reporterName, int tableID, int numberOfRaces, Game game = Game.mk8dx)
        {
            var player = await _context.PlayerGameRegistrations
                .Where(pgr => pgr.Game == (int)game)
                .Select(pgr => pgr.Player)
                .Where(p => p.NormalizedName == PlayerUtils.NormalizeName(playerName))
                .Select(p => new { p.Id, p.Name })
                .SingleOrDefaultAsync();

            if (player is null) 
            { 
                return NotFound();
            }

            var reporter = await _context.PlayerGameRegistrations
                .Where(pgr => pgr.Game == (int)game)
                .Select(pgr => pgr.Player)
                .Where(p => p.NormalizedName == PlayerUtils.NormalizeName(reporterName))
                .Select(p => new { p.Id, p.Name })
                .SingleOrDefaultAsync();

            if (reporter is null)
            {
                return NotFound();
            }

            PenaltyRequest request = new()
            {
                Game = (int)game,
                PenaltyName = penaltyType,
                TableId = tableID,
                NumberOfRaces = numberOfRaces,
                PlayerId = player.Id,
                ReporterId = reporter.Id
            };

            _context.PenaltyRequests.Add(request);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPenaltyRequest), new { id = request.Id }, PenaltyRequestUtils.GetPenaltyRequestDetails(request, player.Name, reporter.Name));

        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id, Game game = Game.mk8dx)
        {
            var requestData = await _context.PenaltyRequests
                .Where(r => r.Id == id && r.Game == (int)game)
                .Select(r => new { PenaltyRequest = r })
                .FirstOrDefaultAsync();

            if (requestData is null)
            {
                return NotFound();
            }

            _context.PenaltyRequests.Remove(requestData.PenaltyRequest);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
