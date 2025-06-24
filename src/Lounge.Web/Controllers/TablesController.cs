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
using Lounge.Web.Data.Entities;
using Lounge.Web.Models.Enums;

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
        public async Task<ActionResult<List<TableDetailsViewModel>>> GetTables(DateTime from, DateTime? to, Game game = Game.mk8dx, int? season = null)
        {
            if (season != null && !_loungeSettingsService.ValidSeasons[game].Contains(season.Value))
                return BadRequest($"Invalid season {season} for game {game}");

            season ??= _loungeSettingsService.CurrentSeason[game];

            var tables = await _context.Tables
                .AsNoTracking()
                .Where(t => t.CreatedOn >= from && (to == null || t.CreatedOn <= to) && t.Season == season && t.Game == (int)game)
                .SelectPropertiesForTableDetails()
                .ToListAsync();

            return tables.Select(t => TableUtils.GetTableDetails(t, _loungeSettingsService)).ToList();
        }

        [HttpGet("unverified")]
        [AllowAnonymous]
        public async Task<ActionResult<List<TableDetailsViewModel>>> GetUnverifiedTables(Game game = Game.mk8dx, int? season = null)
        {
            if (season != null && !_loungeSettingsService.ValidSeasons[game].Contains(season.Value))
                return BadRequest($"Invalid season {season} for game {game}");

            season ??= _loungeSettingsService.CurrentSeason[game];

            var tables = await _context.Tables
                .AsNoTracking()
                .SelectPropertiesForTableDetails()
                .Where(t => t.VerifiedOn == null && t.DeletedOn == null && t.Season == season && t.Game == (int)game)
                .ToListAsync();

            return tables.Select(t => TableUtils.GetTableDetails(t, _loungeSettingsService)).ToList();
        }

        [HttpPost("create")]
        public async Task<ActionResult<TableDetailsViewModel>> Create(NewTableViewModel vm, Game game = Game.mk8dx, bool squadQueue = false)
        {
            var numPlayers = vm.Scores.Count;
            if (game == Game.mk8dx && numPlayers != 12)
                return BadRequest("Must supply 12 scores");

            if (game == Game.mkworld && !(numPlayers == 12 || numPlayers == 24))
                return BadRequest("Must supply 12 or 24 scores");

            var playerNames = vm.Scores.Select(s => s.PlayerName).ToHashSet();
            var normalizedPlayerNames = playerNames.Select(PlayerUtils.NormalizeName).ToHashSet();
            if (playerNames.Count != vm.Scores.Count)
                return BadRequest("Duplicate player name in scores");

            var players = await _context.PlayerGameRegistrations
                .Where(pgr => pgr.Game == (int)game)
                .Select(pgr => pgr.Player)
                .Where(p => normalizedPlayerNames.Contains(p.NormalizedName))
                .ToListAsync();

            if (players.Count != playerNames.Count)
            {
                var foundPlayers = players.Select(p => p.NormalizedName).ToHashSet();
                var invalidPlayers = playerNames.Where(name => !foundPlayers.Contains(PlayerUtils.NormalizeName(name))).ToArray();
                return NotFound($"Invalid players: {string.Join(", ", invalidPlayers)}");
            }

            var playerIdLookup = players.ToDictionary(p => p.Name, p => p.Id);
            var playerCountryCodeLookup = players.ToDictionary(p => p.Name, p => p.CountryCode);

            int numTeams = vm.Scores.Max(s => s.Team) + 1;
            if (numPlayers == 24)
            {
                if (numTeams is not (2 or 3 or 4 or 6 or 8 or 12 or 24))
                    return BadRequest("Invalid number of teams");
            }
            else
            {
                if (numTeams is not (2 or 3 or 4 or 6 or 12))
                    return BadRequest("Invalid number of teams");
            }

            int playersPerTeam = numPlayers / numTeams;

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
                Season = _loungeSettingsService.CurrentSeason[game],
                Game = (int)game
            };

            await _context.Tables.AddAsync(table);
            await _context.SaveChangesAsync();

            await _tableImageService.UploadTableImageAsync(table.Id, tableImage);

            return CreatedAtAction(nameof(GetTable), new { tableId = table.Id }, TableUtils.GetTableDetails(table, _loungeSettingsService));
        }

        [HttpPost("setMultipliers")]
        public async Task<IActionResult> SetMultipliers(int tableId, Game game, [FromBody] Dictionary<string, double> multipliers)
        {
            var table = await _context.Tables
                .Include(t => t.Scores)
                .ThenInclude(t => t.Player)
                .FirstOrDefaultAsync(t => t.Id == tableId && t.Game == (int)game);

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
        public async Task<IActionResult> SetScores(int tableId, Game game, [FromBody] Dictionary<string, int> scores)
        {
            var table = await _context.Tables
                .Include(t => t.Scores)
                .ThenInclude(t => t.Player)
                .FirstOrDefaultAsync(t => t.Id == tableId && t.Game == (int)game);

            if (table is null)
                return NotFound();

            int[]? teamRanks = null;
            if (table.VerifiedOn is not null)
            {
                teamRanks = ScoresToRanks(table.Scores);
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

            if (teamRanks is not null)
            {
                var newTeamRanks = ScoresToRanks(table.Scores);

                if (!teamRanks.SequenceEqual(newTeamRanks))
                {
                    return BadRequest("Table has already been verified and these scores would change the ranking. Please delete and recreate the table instead.");
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

            static int[] ScoresToRanks(ICollection<TableScore> scores)
            {
                int rank = 0;
                int prevRank = 0;
                int prevScore = -1;
                int[] teamOrder = new int[scores.Select(s => s.Team).Max() + 1];
                foreach (var team in scores.GroupBy(s => s.Team).OrderByDescending(g => g.Sum(s => s.Score)))
                {
                    var score = team.Sum(s => s.Score);
                    prevRank = teamOrder[team.Key] = score == prevScore ? prevRank : rank;
                    prevScore = score;
                    rank++;
                }

                return teamOrder;
            }
        }

        [HttpPost("setTableMessageId")]
        public async Task<IActionResult> SetTableMessageId(int tableId, Game game, string tableMessageId)
        {
            var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == tableId && t.Game == (int)game);

            if (table is null)
                return NotFound();

            table.TableMessageId = tableMessageId;

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("setUpdateMessageId")]
        public async Task<IActionResult> SetUpdateMessageId(int tableId, Game game, string updateMessageId)
        {
            var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == tableId && t.Game == (int)game);

            if (table is null)
                return NotFound();

            table.UpdateMessageId = updateMessageId;

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("verify")]
        public async Task<ActionResult<TableDetailsViewModel>> Verify(int tableId, Game game, bool preview=false)
        {
            var table = await _context.Tables
                .Include(t => t.Scores)
                .ThenInclude(t => t.Player.SeasonData)
                .FirstOrDefaultAsync(t => t.Id == tableId && t.Game == (int)game);

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
                s => s.Player.SeasonData.FirstOrDefault(s => s.Season == season && s.Game == (int)game));

            var unplacedPlayers = table.Scores
                .Where(s => seasonDataLookup[s.PlayerId] == null)
                .Select(s => s.Player.Name)
                .ToArray();

            if (unplacedPlayers.Any())
                return BadRequest($"The following players have not been placed yet: {string.Join(", ", unplacedPlayers)}");

            double formatMultiplier = TableUtils.GetFormatMultiplier(table, _loungeSettingsService.SquadQueueMultipliers[game][season]);

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
                        .AsNoTracking()
                        .Where(s => s.PlayerId == score.PlayerId && s.Table.VerifiedOn != null && s.Table.DeletedOn == null && s.Table.Season == season && s.Table.Game == (int)game)
                        .Take(5) // no need to count more than 5
                        .CountAsync();

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
        public async Task<IActionResult> Delete(int tableId, Game game)
        {
            var table = await _context.Tables
                .Include(t => t.Scores)
                .ThenInclude(t => t.Player.SeasonData)
                .FirstOrDefaultAsync(t => t.Id == tableId && t.Game == (int)game);

            if (table is null)
                return NotFound();

            if (table.DeletedOn is not null)
                return BadRequest("Table has already been deleted");

            var season = table.Season;
            if (season != _loungeSettingsService.CurrentSeason[(Game)table.Game])
                return BadRequest("Table is from a previous season and can't be deleted");

            var seasonDataLookup = table.Scores.ToDictionary(
                s => s.PlayerId,
                s => s.Player.SeasonData.FirstOrDefault(s => s.Season == season && s.Game == table.Game));

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
