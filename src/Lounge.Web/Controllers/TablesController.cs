using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using Lounge.Web.Models;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Utils;
using Lounge.Web.Data;
using System;
using Microsoft.AspNetCore.Authorization;

namespace Lounge.Web.Controllers
{
    [Route("api/table")]
    [Authorize]
    [ApiController]
    public class TablesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TablesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<Table>> GetTable(int tableId)
        {
            var table = await _context.Tables
                .Include(t => t.Scores)
                .ThenInclude(s => s.Player)
                .SingleOrDefaultAsync(t => t.Id == tableId);

            if (table is null)
            {
                return NotFound();
            }

            return table;
        }

        [HttpPost("create")]
        public async Task<ActionResult<Table>> Create(NewTableViewModel vm)
        {
            if (vm.Scores.Count != 12)
                return BadRequest("Must supply 12 scores");

            var playerNames = vm.Scores.Select(s => s.PlayerName).ToHashSet();
            var normalizedPlayerNames = playerNames.Select(s => s.ToUpperInvariant()).ToHashSet();
            if (playerNames.Count != vm.Scores.Count)
                return BadRequest("Duplicate player name in scores");

            var players = await _context.Players.Where(p => normalizedPlayerNames.Contains(p.NormalizedName)).ToListAsync();
            if (players.Count != playerNames.Count)
            {
                var foundPlayers = players.Select(p => p.NormalizedName).ToHashSet();
                var invalidPlayers = playerNames.Where(name => !foundPlayers.Contains(name.ToUpperInvariant())).ToArray();
                return NotFound($"Invalid players: {string.Join(", ", invalidPlayers)}");
            }

            var playerIdLookup = players.ToDictionary(p => p.Name, p => p.Id);

            int numTeams = vm.Scores.Max(s => s.Team) + 1;
            if (numTeams is not (2 or 3 or 4 or 6 or 12))
                return BadRequest("Invalid number of teams");

            int playersPerTeam = 12 / numTeams;

            var tableScores = new List<TableScore>();
            foreach (var score in vm.Scores)
            {
                tableScores.Add(new TableScore
                {
                    PlayerId = playerIdLookup[score.PlayerName],
                    Score = score.Score,
                    Team = score.Team,
                    Multiplier = score.Multiplier
                });
            }

            var scores = new (string Player, int Score)[numTeams][];
            for (int i = 0; i < numTeams; i++)
            {
                scores[i] = vm.Scores
                    .Where(score => score.Team == i)
                    .Select(score => (score.PlayerName, score.Score))
                    .ToArray();

                if (scores[i].Length != playersPerTeam)
                    return BadRequest($"Invalid number of players on team {i}: got {scores[i].Length}, expected {playersPerTeam}");
            }

            string tableUrl = TableUtils.BuildUrl(vm.Tier, scores);

            var table = new Table
            {
                CreatedOn = DateTime.UtcNow,
                NumTeams = numTeams,
                Url = tableUrl,
                Tier = vm.Tier,
                Scores = tableScores
            };

            await _context.Tables.AddAsync(table);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTable), new { tableId = table.Id }, table);
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify(int tableId)
        {
            var table = await _context.Tables
                .Include(t => t.Scores)
                .ThenInclude(t => t.Player)
                .FirstOrDefaultAsync(t => t.Id == tableId);

            if (table is null)
                return NotFound();

            if (table.VerifiedOn is not null)
                return BadRequest("Table has already been verified");

            int numTeams = table.NumTeams;
            int playersPerTeam = 12 / numTeams;

            var unplacedPlayers = table.Scores.Where(s => s.Player.Mmr == null).Select(s => s.Player.Name).ToArray();
            if (unplacedPlayers.Any())
                return BadRequest($"The following players have not been placed yet: {string.Join(", ", unplacedPlayers)}");

            var scores = new (string Player, int Score, int CurrentMmr, double Multiplier)[numTeams][];
            for (int i = 0; i < numTeams; i++)
                scores[i] = table.Scores.Where(score => score.Team == i).Select(s => (s.Player.Name, s.Score, s.Player.Mmr!.Value, s.Multiplier)).ToArray();

            var mmrDeltas = TableUtils.GetMMRDeltas(scores);
            foreach (var score in table.Scores)
            {
                var delta = mmrDeltas[score.Player.Name];
                int prevMmr = score.Player.Mmr!.Value;
                int newMmr = prevMmr + delta;
                score.PrevMmr = prevMmr;
                score.NewMmr = newMmr;

                score.Player.Mmr = newMmr;
                score.Player.MaxMmr = Math.Max(score.Player.MaxMmr!.Value, newMmr);
            }

            table.VerifiedOn = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(mmrDeltas);
        }
    }
}
