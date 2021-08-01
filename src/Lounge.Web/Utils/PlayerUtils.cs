using Lounge.Web.Models;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Stats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lounge.Web.Utils
{
    public static class PlayerUtils
    {
        public static string NormalizeName(string name) => string.Join("", name.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

        public static IQueryable<Player> SelectPropertiesForPlayerDetails(this IQueryable<Player> players, int season) =>
            players.Select(p => new Player
            {
                Id = p.Id,
                MKCId = p.MKCId,
                Name = p.Name,
                NormalizedName = p.NormalizedName,
                SeasonData = p.SeasonData
                    .Where(s => s.Season == season)
                    .ToList(),
                Bonuses = p.Bonuses
                    .Where(b => b.Season == season)
                    .Select(b => new Bonus { Id = b.Id, AwardedOn = b.AwardedOn, DeletedOn = b.DeletedOn, NewMmr = b.NewMmr, PrevMmr = b.PrevMmr })
                    .ToList(),
                Penalties = p.Penalties
                    .Where(pen => pen.Season == season)
                    .Select(pen => new Penalty { Id = pen.Id, AwardedOn = pen.AwardedOn, DeletedOn = pen.DeletedOn, NewMmr = pen.NewMmr, PrevMmr = pen.PrevMmr })
                    .ToList(),
                Placements = p.Placements
                    .Where(pl => pl.Season == season)
                    .Select(pl => new Placement { Id = pl.Id, AwardedOn = pl.AwardedOn, Mmr = pl.Mmr, PrevMmr = pl.PrevMmr })
                    .ToList(),
                TableScores = p.TableScores
                    .Where(t => t.Table.Season == season)
                    .Select(t => new TableScore
                    {
                        TableId = t.TableId,
                        NewMmr = t.NewMmr,
                        Score = t.Score,
                        PrevMmr = t.PrevMmr,
                        Team = t.Team,
                        Table = new Table
                        {
                            VerifiedOn = t.Table.VerifiedOn,
                            DeletedOn = t.Table.DeletedOn,
                            NumTeams = t.Table.NumTeams,
                            Scores = t.Table.Scores.Select(s => new TableScore { Score = s.Score, Team = s.Team, PlayerId = s.PlayerId }).ToList()
                        }
                    })
                    .ToList(),
            });

        public static PlayerDetailsViewModel GetPlayerDetails(Player player, RankedPlayerStat rankedPlayerStat, int season)
        {
            (int overallRank, PlayerStat playerStat) = rankedPlayerStat;
            var mmrChanges = new List<PlayerDetailsViewModel.MmrChange>();

            if (player.Placements.Count > 0)
            {
                foreach (var placement in player.Placements)
                {
                    mmrChanges.Add(new PlayerDetailsViewModel.MmrChange(
                        changeId: null,
                        newMmr: placement.Mmr,
                        mmrDelta: placement.PrevMmr is int prevMmr ? placement.Mmr - prevMmr : 0,
                        reason: PlayerDetailsViewModel.MmrChangeReason.Placement,
                        time: placement.AwardedOn));
                }
            }

            var allPartnerScores = new List<int>();
            var allScores = new List<int>();

            foreach (var tableScore in player.TableScores)
            {
                if (tableScore.Table.VerifiedOn is null)
                    continue;

                var newMmr = tableScore.NewMmr!.Value;
                var delta = newMmr - tableScore.PrevMmr!.Value;

                int numTeams = tableScore.Table.NumTeams;
                int[] teamTotals = new int[tableScore.Table.NumTeams];
                foreach (var score in tableScore.Table.Scores)
                    teamTotals[score.Team] += score.Score;

                int playerTeamTotal = teamTotals[tableScore.Team];
                Array.Sort(teamTotals);
                var rank = numTeams - Array.LastIndexOf(teamTotals, playerTeamTotal);

                var partnerScores = tableScore.Table.Scores
                    .Where(s => s.Team == tableScore.Team && s.PlayerId != player.Id)
                    .Select(s => s.Score)
                    .ToList();

                mmrChanges.Add(new PlayerDetailsViewModel.MmrChange(
                    changeId: tableScore.TableId,
                    newMmr: newMmr,
                    mmrDelta: delta,
                    reason: PlayerDetailsViewModel.MmrChangeReason.Table,
                    time: tableScore.Table.VerifiedOn!.Value,
                    score: tableScore.Score,
                    partnerScores: partnerScores,
                    rank: rank));

                if (tableScore.Table.DeletedOn is not null)
                {
                    mmrChanges.Add(new PlayerDetailsViewModel.MmrChange(
                        changeId: tableScore.TableId,
                        newMmr: -1,
                        mmrDelta: -delta,
                        reason: PlayerDetailsViewModel.MmrChangeReason.TableDelete,
                        time: tableScore.Table.DeletedOn!.Value));
                }
                else
                {
                    allPartnerScores.AddRange(partnerScores);
                    allScores.Add(tableScore.Score);
                }
            }

            foreach (var penalty in player.Penalties)
            {
                var newMmr = penalty.NewMmr;
                var delta = newMmr - penalty.PrevMmr;

                mmrChanges.Add(new PlayerDetailsViewModel.MmrChange(
                    changeId: penalty.Id,
                    newMmr: newMmr,
                    mmrDelta: delta,
                    reason: penalty.IsStrike ? PlayerDetailsViewModel.MmrChangeReason.Strike : PlayerDetailsViewModel.MmrChangeReason.Penalty,
                    time: penalty.AwardedOn));

                if (penalty.DeletedOn is not null)
                {
                    mmrChanges.Add(new PlayerDetailsViewModel.MmrChange(
                        changeId: penalty.Id,
                        newMmr: -1,
                        mmrDelta: -delta,
                        reason: penalty.IsStrike ? PlayerDetailsViewModel.MmrChangeReason.StrikeDelete : PlayerDetailsViewModel.MmrChangeReason.PenaltyDelete,
                        time: penalty.DeletedOn.Value));
                }
            }

            foreach (var bonus in player.Bonuses)
            {
                var newMmr = bonus.NewMmr;
                var delta = newMmr - bonus.PrevMmr;

                mmrChanges.Add(new PlayerDetailsViewModel.MmrChange(
                    changeId: bonus.Id,
                    newMmr: newMmr,
                    mmrDelta: delta,
                    reason: PlayerDetailsViewModel.MmrChangeReason.Bonus,
                    time: bonus.AwardedOn));

                if (bonus.DeletedOn is not null)
                {
                    mmrChanges.Add(new PlayerDetailsViewModel.MmrChange(
                        changeId: bonus.Id,
                        newMmr: -1,
                        mmrDelta: -delta,
                        reason: PlayerDetailsViewModel.MmrChangeReason.BonusDelete,
                        time: bonus.DeletedOn.Value));
                }
            }

            mmrChanges = mmrChanges.OrderBy(c => c.Time).ToList();

            int mmr = 0;
            foreach (var change in mmrChanges)
            {
                if (change.Reason is PlayerDetailsViewModel.MmrChangeReason.TableDelete or PlayerDetailsViewModel.MmrChangeReason.PenaltyDelete)
                {
                    change.NewMmr = Math.Max(0, mmr + change.MmrDelta);
                    change.MmrDelta = change.NewMmr - mmr;
                }

                mmr = change.NewMmr;
            }

            if (mmrChanges.Count > 0)
            {
                var changesToRemove = new HashSet<int>();
                var prev = mmrChanges[0];
                for (int i = 1; i < mmrChanges.Count; i++)
                {
                    var cur = mmrChanges[i];
                    if (prev.ChangeId == cur.ChangeId)
                    {
                        switch ((prev.Reason, cur.Reason))
                        {
                            case (PlayerDetailsViewModel.MmrChangeReason.Table, PlayerDetailsViewModel.MmrChangeReason.TableDelete):
                            case (PlayerDetailsViewModel.MmrChangeReason.Penalty, PlayerDetailsViewModel.MmrChangeReason.PenaltyDelete):
                            case (PlayerDetailsViewModel.MmrChangeReason.Strike, PlayerDetailsViewModel.MmrChangeReason.StrikeDelete):
                            case (PlayerDetailsViewModel.MmrChangeReason.Bonus, PlayerDetailsViewModel.MmrChangeReason.BonusDelete):
                                changesToRemove.Add(i - 1);
                                changesToRemove.Add(i);
                                break;
                        }
                    }

                    prev = cur;
                }

                if (changesToRemove.Count > 0)
                {
                    var newChanges = new List<PlayerDetailsViewModel.MmrChange>();
                    for (int i = 0; i < mmrChanges.Count; i++)
                    {
                        if (!changesToRemove.Contains(i))
                            newChanges.Add(mmrChanges[i]);
                    }
                    mmrChanges = newChanges;
                }
            }

            // sort descending
            mmrChanges.Reverse();

            decimal? winRate = playerStat.EventsPlayed == 0 ? null : (decimal)playerStat.Wins / playerStat.EventsPlayed;

            var largestGain = playerStat.LargestGain < 0 ? null : playerStat.LargestGain;
            var largestLoss = playerStat.LargestLoss > 0 ? null : playerStat.LargestLoss;

            var seasonData = player.SeasonData.FirstOrDefault();

            var vm = new PlayerDetailsViewModel
            {
                PlayerId = player.Id,
                Name = player.Name,
                MkcId = player.MKCId,
                Season = season,
                Mmr = seasonData?.Mmr,
                MaxMmr = seasonData?.MaxMmr,
                OverallRank = overallRank,
                MmrChanges = mmrChanges,
                EventsPlayed = playerStat.EventsPlayed,
                WinRate = winRate,
                WinsLastTen = playerStat.LastTenWins,
                LossesLastTen = playerStat.LastTenLosses,
                GainLossLastTen = playerStat.LastTenGainLoss,
                LargestGain = largestGain,
                LargestGainTableId = largestGain == null ? null : mmrChanges.FirstOrDefault(c => c.MmrDelta == largestGain)?.ChangeId,
                LargestLoss = largestLoss,
                LargestLossTableId = largestLoss == null ? null : mmrChanges.FirstOrDefault(c => c.MmrDelta == largestLoss)?.ChangeId,
                AverageScore = allScores.Count == 0 ? null : allScores.Average(),
                AverageLastTen = allScores.Count == 0 ? null : allScores.TakeLast(10).Average(),
                PartnerAverage = allPartnerScores.Count == 0 ? null : allPartnerScores.Average()
            };

            return vm;
        }

        public static PlayerViewModel GetPlayerViewModel(Player player, PlayerSeasonData? seasonData)
        {
            return new PlayerViewModel
            {
                Id = player.Id,
                DiscordId = player.DiscordId,
                MKCId = player.MKCId,
                Name = player.Name,
                Mmr = seasonData?.Mmr,
                MaxMmr = seasonData?.MaxMmr,
            };
        }
    }
}
