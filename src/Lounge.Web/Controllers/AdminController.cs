using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Lounge.Web.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Utils;
using Microsoft.Extensions.Logging;
using Lounge.Web.Storage;

namespace Lounge.Web.Controllers
{
    [Route("api/admin")]
    [Authorize]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly ITableImageService _tableImageService;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger, ITableImageService tableImageService)
        {
            _context = context;
            _logger = logger;
            _tableImageService = tableImageService;
        }

        [HttpPost("saveTablesToBlob")]
        public async Task<IActionResult> SaveTablesToBlob(int? tableIdStart = null, int? tableIdEnd = null)
        {
            int idStart = tableIdStart ?? 0;
            int idEnd = tableIdEnd ?? await _context.Tables.MaxAsync(t => t.Id);

            for (int i = idStart; i < idEnd; i += 25)
            {
                var start = i;
                var end = Math.Min(idEnd, i + 24);
                var tables = await _context.Tables.AsNoTracking()
                    .Where(t => (t.Id >= start) && (t.Id <= end) && t.TableImageData != null)
                    .Select(t => new { t.Id, t.TableImageData })
                    .ToListAsync();

                foreach (var table in tables)
                {
                    if (table.TableImageData != null)
                    {
                        var bytes = Convert.FromBase64String(table.TableImageData);
                        await _tableImageService.UploadTableImageAsync(table.Id, bytes);
                    }
                }
            }

            return Ok();
        }

        [HttpPost("fixMmr")]
        public async Task<IActionResult> FixAllMmr(bool preview=true)
        {
            var players = await _context.Players.ToDictionaryAsync(p => p.Id);
            var placements = await _context.Placements.ToDictionaryAsync(p => p.Id);
            var penalties = await _context.Penalties.ToDictionaryAsync(p => p.Id);
            var bonuses = await _context.Bonuses.ToDictionaryAsync(b => b.Id);
            var tables = await _context.Tables.AsNoTracking().Select(t => new { t.Id, t.VerifiedOn, t.DeletedOn, t.NumTeams, t.Tier }).ToDictionaryAsync(t => t.Id);
            var tableScores = await _context.TableScores.ToListAsync();

            var tableScoreMap = tableScores.GroupBy(t => t.TableId).ToDictionary(t => t.Key, t => t.ToList());

            var matchesPlayed = new Dictionary<int, int>();
            var playerMmrs = new Dictionary<int, int>();
            var playerMaxMmrs = new Dictionary<int, int?>();
            foreach (var player in players.Values)
            {
                if (player.Mmr != null)
                {
                    matchesPlayed[player.Id] = 0;
                }
            }

            var events = new List<(DateTimeOffset Time, PlayerDetailsViewModel.MmrChangeReason Reason, int EntityId)>();

            foreach (var placement in placements.Values)
            {
                events.Add((placement.AwardedOn, PlayerDetailsViewModel.MmrChangeReason.Placement, placement.Id));
            }

            foreach (var penalty in penalties.Values)
            {
                events.Add((penalty.AwardedOn, PlayerDetailsViewModel.MmrChangeReason.Penalty, penalty.Id));
                if (penalty.DeletedOn != null)
                {
                    events.Add((penalty.DeletedOn.Value, PlayerDetailsViewModel.MmrChangeReason.PenaltyDelete, penalty.Id));
                }
            }

            foreach (var bonus in bonuses.Values)
            {
                events.Add((bonus.AwardedOn, PlayerDetailsViewModel.MmrChangeReason.Bonus, bonus.Id));
                if (bonus.DeletedOn != null)
                {
                    events.Add((bonus.DeletedOn.Value, PlayerDetailsViewModel.MmrChangeReason.BonusDelete, bonus.Id));
                }
            }

            foreach (var table in tables.Values)
            {
                if (table.VerifiedOn != null)
                {
                    events.Add((table.VerifiedOn.Value, PlayerDetailsViewModel.MmrChangeReason.Table, table.Id));
                    if (table.DeletedOn != null)
                    {
                        events.Add((table.DeletedOn.Value, PlayerDetailsViewModel.MmrChangeReason.TableDelete, table.Id));
                    }
                }
            }

            events = events.OrderBy(e => e.Time).ToList();

            var newTableScoreMmrs = new Dictionary<(int TableId, int PlayerId), (int PrevMmr, int NewMmr)>();
            var newPenaltyMmrs = new Dictionary<int, (int PrevMmr, int NewMmr)>();
            var newBonusMmrs = new Dictionary<int, (int PrevMmr, int NewMmr)>();

            foreach (var (time, reason, entityId) in events)
            {
                switch (reason)
                {
                    case PlayerDetailsViewModel.MmrChangeReason.Placement:
                        {
                            var placement = placements[entityId];
                            playerMmrs[placement.PlayerId] = placement.Mmr;
                        }
                        break;
                    case PlayerDetailsViewModel.MmrChangeReason.Table:
                        {
                            var table = tables[entityId];
                            int numTeams = table.NumTeams;

                            double sqMultiplier = string.Equals(table.Tier, "SQ", StringComparison.OrdinalIgnoreCase) ? 0.75 : 1;

                            var scores = new (string Player, int Score, int CurrentMmr, double Multiplier)[numTeams][];
                            for (int i = 0; i < numTeams; i++)
                                scores[i] = tableScoreMap[table.Id].Where(score => score.Team == i).Select(s => (s.PlayerId.ToString(), s.Score, playerMmrs.GetValueOrDefault(s.PlayerId, s.PrevMmr!.Value), sqMultiplier * s.Multiplier)).ToArray();

                            var mmrDeltas = TableUtils.GetMMRDeltas(scores);
                            foreach (var score in tableScoreMap[table.Id])
                            {
                                var delta = mmrDeltas[score.PlayerId.ToString()];
                                int prevMmr = playerMmrs.GetValueOrDefault(score.PlayerId, score.PrevMmr!.Value);
                                int newMmr = prevMmr + delta;

                                if (prevMmr != score.PrevMmr || score.NewMmr != newMmr)
                                {
                                    newTableScoreMmrs[(entityId, score.PlayerId)] = (prevMmr, newMmr);
                                }

                                playerMmrs[score.PlayerId] = newMmr;
                                if (playerMaxMmrs.TryGetValue(score.PlayerId, out int? maxMmrNullable) && maxMmrNullable is int maxMmr)
                                {
                                    playerMaxMmrs[score.PlayerId] = Math.Max(maxMmr, newMmr);
                                }
                                else
                                {
                                    if (matchesPlayed[score.PlayerId] >= 4)
                                        playerMaxMmrs[score.PlayerId] = newMmr;
                                }

                                matchesPlayed[score.PlayerId]++;
                            }
                        }
                        break;
                    case PlayerDetailsViewModel.MmrChangeReason.Penalty:
                        {
                            var penalty = penalties[entityId];
                            int amount = penalty.NewMmr - penalty.PrevMmr;
                            int prevMmr = playerMmrs[penalty.PlayerId];
                            int newMmr = Math.Max(0, penalty.PrevMmr + amount);
                            playerMmrs[penalty.PlayerId] = penalty.NewMmr;

                            if (prevMmr != penalty.PrevMmr || penalty.NewMmr != newMmr)
                            {
                                newPenaltyMmrs[entityId] = (prevMmr, newMmr);
                            }
                        }
                        break;
                    case PlayerDetailsViewModel.MmrChangeReason.Bonus:
                        {
                            var bonus = bonuses[entityId];
                            int amount = bonus.NewMmr - bonus.PrevMmr;
                            int prevMmr = playerMmrs[bonus.PlayerId];
                            int newMmr = Math.Max(0, bonus.PrevMmr + amount);
                            playerMmrs[bonus.PlayerId] = bonus.NewMmr;

                            if (prevMmr != bonus.PrevMmr || bonus.NewMmr != newMmr)
                            {
                                newBonusMmrs[entityId] = (prevMmr, newMmr);
                            }
                        }
                        break;
                    case PlayerDetailsViewModel.MmrChangeReason.TableDelete:
                        {
                            var table = tables[entityId];
                            foreach (var score in tableScoreMap[table.Id])
                            {
                                var curMMR = playerMmrs[score.PlayerId];
                                var diff = newTableScoreMmrs.TryGetValue((entityId, score.PlayerId), out var newScore)
                                    ? newScore.NewMmr - newScore.PrevMmr
                                    : score.NewMmr!.Value - score.PrevMmr!.Value;

                                var newMMR = Math.Max(0, curMMR - diff);
                                playerMmrs[score.PlayerId] = newMMR;

                                if (playerMaxMmrs.TryGetValue(score.PlayerId, out int? maxMmrNullable) && maxMmrNullable is int maxMmr)
                                    playerMaxMmrs[score.PlayerId] = Math.Max(maxMmr, newMMR);

                                matchesPlayed[score.PlayerId]--;
                            }
                        }
                        break;
                    case PlayerDetailsViewModel.MmrChangeReason.PenaltyDelete:
                        {
                            var penalty = penalties[entityId];
                            int amount = newPenaltyMmrs.TryGetValue(entityId, out var newPenalty)
                                ? newPenalty.NewMmr - newPenalty.PrevMmr
                                : penalty.NewMmr - penalty.PrevMmr;

                            playerMmrs[penalty.PlayerId] = Math.Max(playerMmrs[penalty.PlayerId] - amount, 0);
                        }
                        break;
                    case PlayerDetailsViewModel.MmrChangeReason.BonusDelete:
                        {
                            var bonus = bonuses[entityId];
                            int amount = newBonusMmrs.TryGetValue(entityId, out var newBonus)
                                ? newBonus.NewMmr - newBonus.PrevMmr
                                : bonus.NewMmr - bonus.PrevMmr;

                            playerMmrs[bonus.PlayerId] = Math.Max(playerMmrs[bonus.PlayerId] - amount, 0);
                        }
                        break;
                }
            }

            var newMmrs = new Dictionary<int, int>();
            var newMaxMmrs = new Dictionary<int, int?>();

            var mmrChanges = new Dictionary<string, object>();

            foreach (var player in players.Values)
            {
                if (!playerMmrs.ContainsKey(player.Id))
                    continue;

                var expectedMmr = playerMmrs[player.Id];
                var expectedMaxMmr = playerMaxMmrs.GetValueOrDefault(player.Id, null);

                if (player.Mmr != expectedMmr)
                {
                    if (RankUtils.GetRank(player.Mmr) != RankUtils.GetRank(expectedMmr))
                        _logger.LogInformation($"{player.Name} ({player.Id}): {RankUtils.GetRank(player.Mmr)} -> {RankUtils.GetRank(expectedMmr)}");

                    newMmrs[player.Id] = expectedMmr;
                    mmrChanges[player.Name] = new { PrevMmr = player.Mmr, NewMmr = expectedMmr };
                }

                if (player.MaxMmr != expectedMaxMmr)
                {
                    newMaxMmrs[player.Id] = expectedMaxMmr;
                }
            }


            if (!preview)
            {
                foreach ((int id, (int prevMmr, int newMmr)) in newPenaltyMmrs)
                {
                    penalties[id].PrevMmr = prevMmr;
                    penalties[id].NewMmr = newMmr;
                }

                foreach ((int id, (int prevMmr, int newMmr)) in newBonusMmrs)
                {
                    bonuses[id].PrevMmr = prevMmr;
                    bonuses[id].NewMmr = newMmr;
                }

                foreach (((int tableId, int playerId), (int prevMmr, int newMmr)) in newTableScoreMmrs)
                {
                    tableScoreMap[tableId].Single(s => s.PlayerId == playerId).PrevMmr = prevMmr;
                    tableScoreMap[tableId].Single(s => s.PlayerId == playerId).NewMmr = newMmr;
                }

                foreach ((int id, int mmr) in newMmrs)
                {
                    players[id].Mmr = mmr;
                }

                foreach ((int id, int? maxMmr) in newMaxMmrs)
                {
                    players[id].MaxMmr = maxMmr;
                }

                await _context.SaveChangesAsync();
            }

            return Ok(mmrChanges);
        }
    }
}
