using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Utils;
using Lounge.Web.Data;
using System;
using Microsoft.AspNetCore.Authorization;
using Lounge.Web.Storage;
using Lounge.Web.Settings;
using Lounge.Web.Controllers.ValidationAttributes;
using Lounge.Web.Data.Entities;

namespace Lounge.Web.Controllers
{
    [Route("api/table")]
    [Authorize]
    [ApiController]
    public class TablesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITableImageService _tableImageService;
        private readonly ILoungeSettingsService _loungeSettingsService;

        public TablesController(ApplicationDbContext context, ITableImageService tableImageService, ILoungeSettingsService loungeSettingsService)
        {
            _context = context;
            _tableImageService = tableImageService;
            _loungeSettingsService = loungeSettingsService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<TableDetailsViewModel>> GetTable(int tableId)
        {
            var table = await _context.Tables
                .AsNoTracking()
                .SelectPropertiesForTableDetails()
                .SingleOrDefaultAsync(t => t.Id == tableId);

            if (table is null)
                return NotFound();

            return TableUtils.GetTableDetails(table, _loungeSettingsService);
        }

        [HttpGet("list")]
        [AllowAnonymous]
        public async Task<ActionResult<List<TableDetailsViewModel>>> GetTables(DateTime from, DateTime? to, [ValidSeason] int? season = null)
        {
            season ??= _loungeSettingsService.CurrentSeason;

            var tables = await _context.Tables
                .AsNoTracking()
                .Where(t => t.CreatedOn >= from && (to == null || t.CreatedOn <= to) && t.Season == season)
                .SelectPropertiesForTableDetails()
                .ToListAsync();

            return tables.Select(t => TableUtils.GetTableDetails(t, _loungeSettingsService)).ToList();
        }

        [HttpGet("unverified")]
        [AllowAnonymous]
        public async Task<ActionResult<List<TableDetailsViewModel>>> GetUnverifiedTables([ValidSeason] int? season = null)
        {
            season ??= _loungeSettingsService.CurrentSeason;

            var tables = await _context.Tables
                .AsNoTracking()
                .SelectPropertiesForTableDetails()
                .Where(t => t.VerifiedOn == null && t.DeletedOn == null && t.Season == season)
                .ToListAsync();

            return tables.Select(t => TableUtils.GetTableDetails(t, _loungeSettingsService)).ToList();
        }

        [HttpPost("create")]
        public async Task<ActionResult<TableDetailsViewModel>> Create(NewTableViewModel vm, bool squadQueue = false)
        {
            if (vm.Scores.Count != 12)
                return BadRequest("Must supply 12 scores");

            var playerNames = vm.Scores.Select(s => s.PlayerName).ToHashSet();
            var normalizedPlayerNames = playerNames.Select(PlayerUtils.NormalizeName).ToHashSet();
            if (playerNames.Count != vm.Scores.Count)
                return BadRequest("Duplicate player name in scores");

            var players = await _context.Players.Where(p => normalizedPlayerNames.Contains(p.NormalizedName)).ToListAsync();
            if (players.Count != playerNames.Count)
            {
                var foundPlayers = players.Select(p => p.NormalizedName).ToHashSet();
                var invalidPlayers = playerNames.Where(name => !foundPlayers.Contains(PlayerUtils.NormalizeName(name))).ToArray();
                return NotFound($"Invalid players: {string.Join(", ", invalidPlayers)}");
            }

            var playerIdLookup = players.ToDictionary(p => p.Name, p => p.Id);
            var playerCountryCodeLookup = players.ToDictionary(p => p.Name, p => p.CountryCode);

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

            var scores = new (string Player, string? CountryCode, int Score)[numTeams][];
            for (int i = 0; i < numTeams; i++)
            {
                scores[i] = vm.Scores
                    .Where(score => score.Team == i)
                    .Select(score => (score.PlayerName, playerCountryCodeLookup[score.PlayerName] , score.Score))
                    .ToArray();

                if (scores[i].Length != playersPerTeam)
                    return BadRequest($"Invalid number of players on team {i}: got {scores[i].Length}, expected {playersPerTeam}");
            }

            string tableUrl = TableUtils.BuildUrl(vm.Tier, scores);
            var tableImage = await TableUtils.GetImageDataAsync(tableUrl);

            var table = new Table
            {
                CreatedOn = DateTime.UtcNow,
                NumTeams = numTeams,
                Tier = vm.Tier,
                Scores = tableScores,
                AuthorId = vm.AuthorId,
                Season = _loungeSettingsService.CurrentSeason,
            };

            await _context.Tables.AddAsync(table);
            await _context.SaveChangesAsync();

            await _tableImageService.UploadTableImageAsync(table.Id, tableImage);

            return CreatedAtAction(nameof(GetTable), new { tableId = table.Id }, TableUtils.GetTableDetails(table, _loungeSettingsService));
        }

        [HttpPost("setMultipliers")]
        public async Task<IActionResult> SetMultipliers(int tableId, [FromBody] Dictionary<string, double> multipliers)
        {
            var table = await _context.Tables
                .Include(t => t.Scores)
                .ThenInclude(t => t.Player)
                .FirstOrDefaultAsync(t => t.Id == tableId);

            if (table is null)
                return NotFound();

            if (table.VerifiedOn is not null)
                return BadRequest("Table has already been verified");

            foreach ((string name, double multiplier) in multipliers)
            {
                bool foundPlayer = false;
                foreach (var score in table.Scores)
                {
                    if (PlayerUtils.NormalizeName(name) == score.Player.NormalizedName)
                    {
                        foundPlayer = true;
                        score.Multiplier = multiplier;
                        break;
                    }
                }

                if (!foundPlayer)
                    return BadRequest($"Player '{name}' is not in table");
            }

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("setScores")]
        public async Task<IActionResult> SetScores(int tableId, [FromBody] Dictionary<string, int> scores)
        {
            var table = await _context.Tables
                .Include(t => t.Scores)
                .ThenInclude(t => t.Player)
                .FirstOrDefaultAsync(t => t.Id == tableId);

            if (table is null)
                return NotFound();

            int[]? teamTotals = null;
            if (table.VerifiedOn is not null)
            {
                teamTotals = table.Scores.GroupBy(s => s.Team).OrderBy(g => g.Key).Select(g => g.Sum(s => s.Score)).ToArray();
            }

            foreach ((string name, int score) in scores)
            {
                bool foundPlayer = false;
                foreach (var tableScore in table.Scores)
                {
                    if (PlayerUtils.NormalizeName(name) == tableScore.Player.NormalizedName)
                    {
                        foundPlayer = true;
                        tableScore.Score = score;
                        break;
                    }
                }

                if (!foundPlayer)
                    return BadRequest($"Player '{name}' is not in table");
            }

            if (teamTotals is not null)
            {
                var newTeamTotals = table.Scores.GroupBy(s => s.Team).OrderBy(g => g.Key).Select(g => g.Sum(s => s.Score)).ToArray();

                if (!teamTotals.SequenceEqual(newTeamTotals))
                {
                    return BadRequest("Table has already been verified and these scores would change the MMR differences. Please delete and recreate the table instead.");
                }
            }

            int numTeams = table.NumTeams;
            var newScores = new (string Player, string? CountryCode, int Score)[numTeams][];
            for (int i = 0; i < numTeams; i++)
            {
                newScores[i] = table.Scores
                    .Where(score => score.Team == i)
                    .Select(score => (score.Player.Name, score.Player.CountryCode, score.Score))
                    .ToArray();
            }

            string tableUrl = TableUtils.BuildUrl(table.Tier, newScores);
            var tableImage = await TableUtils.GetImageDataAsync(tableUrl);

            await _tableImageService.UploadTableImageAsync(tableId, tableImage);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("setTableMessageId")]
        public async Task<IActionResult> SetTableMessageId(int tableId, string tableMessageId)
        {
            var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == tableId);

            if (table is null)
                return NotFound();

            table.TableMessageId = tableMessageId;

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("setUpdateMessageId")]
        public async Task<IActionResult> SetUpdateMessageId(int tableId, string updateMessageId)
        {
            var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == tableId);

            if (table is null)
                return NotFound();

            table.UpdateMessageId = updateMessageId;

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("verify")]
        public async Task<ActionResult<TableDetailsViewModel>> Verify(int tableId, bool preview=false)
        {
            var table = await _context.Tables
                .Include(t => t.Scores)
                .ThenInclude(t => t.Player.SeasonData)
                .FirstOrDefaultAsync(t => t.Id == tableId);

            if (table is null)
                return NotFound();

            if (table.VerifiedOn is not null)
                return BadRequest("Table has already been verified");

            if (table.DeletedOn is not null)
                return BadRequest("Table has been deleted and can't be verified");

            int numTeams = table.NumTeams;

            var season = table.Season;
            var seasonDataLookup = table.Scores.ToDictionary(
                s => s.PlayerId,
                s => s.Player.SeasonData.FirstOrDefault(s => s.Season == season));

            var unplacedPlayers = table.Scores
                .Where(s => seasonDataLookup[s.PlayerId] == null)
                .Select(s => s.Player.Name)
                .ToArray();

            if (unplacedPlayers.Any())
                return BadRequest($"The following players have not been placed yet: {string.Join(", ", unplacedPlayers)}");

            double formatMultiplier = TableUtils.GetFormatMultiplier(table, _loungeSettingsService.SquadQueueMultipliers[season]);

            var scores = new (string Player, int Score, int CurrentMmr, double Multiplier)[numTeams][];
            for (int i = 0; i < numTeams; i++)
            {
                scores[i] = table.Scores
                    .Where(score => score.Team == i)
                    .Select(s => (s.Player.Name, s.Score, seasonDataLookup[s.PlayerId]!.Mmr, formatMultiplier * s.Multiplier))
                    .ToArray();
            }

            var playersWithNewPeakMmr = new HashSet<int>();
            var mmrDeltas = TableUtils.GetMMRDeltas(scores);
            foreach (var score in table.Scores)
            {
                var delta = mmrDeltas[score.Player.Name];
                var seasonData = seasonDataLookup[score.PlayerId]!;
                int prevMmr = seasonData.Mmr;
                int newMmr = prevMmr + delta;
                score.PrevMmr = prevMmr;
                score.NewMmr = newMmr;

                seasonData.Mmr = newMmr;
                if (seasonData.MaxMmr is int maxMmr)
                {
                    if (newMmr > maxMmr)
                    {
                        seasonData.MaxMmr = newMmr;
                        playersWithNewPeakMmr.Add(score.PlayerId);
                    }
                }
                else
                {
                    var playerTotalMatches = await _context.TableScores
                        .CountAsync(s => s.PlayerId == score.PlayerId && s.Table.VerifiedOn != null && s.Table.DeletedOn == null && s.Table.Season == season);

                    if (playerTotalMatches >= 4)
                    {
                        seasonData.MaxMmr = newMmr;
                        playersWithNewPeakMmr.Add(score.PlayerId);
                    }
                }
            }

            if (!preview)
            {
                table.VerifiedOn = DateTime.UtcNow;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new Exception("Table update failed because a player's MMR changed during the update. Please try again.");
                }
            }

            var vm = TableUtils.GetTableDetails(table, _loungeSettingsService);

            foreach (var team in vm.Teams)
            {
                foreach (var score in team.Scores)
                {
                    score.IsNewPeakMmr = playersWithNewPeakMmr.Contains(score.PlayerId);
                }
            }

            return Ok(vm);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int tableId)
        {
            var table = await _context.Tables
                .Include(t => t.Scores)
                .ThenInclude(t => t.Player.SeasonData)
                .FirstOrDefaultAsync(t => t.Id == tableId);

            if (table is null)
                return NotFound();

            if (table.DeletedOn is not null)
                return BadRequest("Table has already been deleted");

            var season = table.Season;
            if (season != _loungeSettingsService.CurrentSeason)
                return BadRequest("Table is from a previous season and can't be deleted");

            var seasonDataLookup = table.Scores.ToDictionary(
                s => s.PlayerId,
                s => s.Player.SeasonData.FirstOrDefault(s => s.Season == season));

            table.DeletedOn = DateTime.UtcNow;

            if (table.VerifiedOn is not null)
            {
                foreach (var score in table.Scores)
                {
                    var seasonData = seasonDataLookup[score.PlayerId]!;
                    var curMMR = seasonData.Mmr;
                    var diff = score.NewMmr!.Value - score.PrevMmr!.Value;
                    var newMMR = Math.Max(0, curMMR - diff);
                    seasonData.Mmr = newMMR;
                    if (seasonData.MaxMmr is int maxMmr)
                    {
                        seasonData.MaxMmr = Math.Max(maxMmr, newMMR);
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
