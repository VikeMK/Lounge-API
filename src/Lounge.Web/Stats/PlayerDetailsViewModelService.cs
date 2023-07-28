using Lounge.Web.Data.ChangeTracking;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lounge.Web.Stats
{
    public class PlayerDetailsViewModelService : IPlayerDetailsViewModelService
    {
        private readonly ILoungeSettingsService _loungeSettingsService;
        private readonly IPlayerStatCache _playerStatsCache;
        private readonly IPlayerDetailsCache _playerDetailsCache;
        private readonly IDbCache _dbCache;

        public PlayerDetailsViewModelService(
            ILoungeSettingsService loungeSettingsService,
            IPlayerStatCache playerStatsCache,
            IPlayerDetailsCache playerDetailsCache,
            IDbCache dbCache)
        {
            _playerStatsCache = playerStatsCache;
            _playerDetailsCache = playerDetailsCache;
            _loungeSettingsService = loungeSettingsService;
            _dbCache = dbCache;
        }

        public PlayerDetailsViewModel? GetPlayerDetails(int playerId, int season)
        {
            if (!_playerStatsCache.TryGetPlayerStatsById(playerId, season, out var playerData))
                return null;

            if (!_playerDetailsCache.TryGetPlayerDetailsById(playerId, season, out var player))
                return null;

            var mmrChanges = new List<PlayerDetailsViewModel.MmrChange>();

            foreach (var placementId in player.PlacementIds)
            {
                var placement = _dbCache.Placements[placementId];
                mmrChanges.Add(new PlayerDetailsViewModel.MmrChange(
                    changeId: null,
                    newMmr: placement.Mmr,
                    mmrDelta: placement.PrevMmr is int prevMmr ? placement.Mmr - prevMmr : 0,
                    reason: PlayerDetailsViewModel.MmrChangeReason.Placement,
                    time: placement.AwardedOn));
            }

            foreach (var tableId in player.TableIds)
            {
                var table = _dbCache.Tables[tableId];
                var tableScores = _dbCache.TableScores[tableId];
                var tableScore = tableScores[playerId];
                if (table.VerifiedOn is null)
                    continue;

                var newMmr = tableScore.NewMmr!.Value;
                var delta = newMmr - tableScore.PrevMmr!.Value;

                int numTeams = table.NumTeams;
                int[] teamTotals = new int[table.NumTeams];
                foreach (var score in tableScores.Values)
                    teamTotals[score.Team] += score.Score;

                int playerTeamTotal = teamTotals[tableScore.Team];
                Array.Sort(teamTotals);
                var rank = numTeams - Array.LastIndexOf(teamTotals, playerTeamTotal);

                List<int> partnerScores = new();
                List<int> partnerIds = new();

                foreach (var score in tableScores.Values)
                {
                    if (score.Team == tableScore.Team && score.PlayerId != playerId)
                    {
                        partnerScores.Add(score.Score);
                        partnerIds.Add(score.PlayerId);
                    }
                }

                mmrChanges.Add(new PlayerDetailsViewModel.MmrChange(
                    changeId: tableScore.TableId,
                    newMmr: newMmr,
                    mmrDelta: delta,
                    reason: PlayerDetailsViewModel.MmrChangeReason.Table,
                    time: table.VerifiedOn!.Value,
                    score: tableScore.Score,
                    partnerScores: partnerScores,
                    partnerIds: partnerIds,
                    rank: rank,
                    tier: table.Tier,
                    numTeams: table.NumTeams));

                if (table.DeletedOn is not null)
                {
                    mmrChanges.Add(new PlayerDetailsViewModel.MmrChange(
                        changeId: tableScore.TableId,
                        newMmr: -1,
                        mmrDelta: -delta,
                        reason: PlayerDetailsViewModel.MmrChangeReason.TableDelete,
                        time: table.DeletedOn!.Value,
                        tier: table.Tier));
                }
            }

            foreach (var penaltyId in player.PenaltyIds)
            {
                var penalty = _dbCache.Penalties[penaltyId];
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

            foreach (var bonusId in player.BonusIds)
            {
                var bonus = _dbCache.Bonuses[bonusId];
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
                if (change.Reason is PlayerDetailsViewModel.MmrChangeReason.TableDelete or PlayerDetailsViewModel.MmrChangeReason.PenaltyDelete or PlayerDetailsViewModel.MmrChangeReason.StrikeDelete or PlayerDetailsViewModel.MmrChangeReason.BonusDelete)
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

            var nameHistory = player.NameChangeIds
                .Select(nameChangeId => _dbCache.NameChanges[nameChangeId])
                .Select(nameChange => new PlayerDetailsViewModel.NameChange(nameChange.Name, nameChange.ChangedOn))
                .OrderByDescending(nc => nc.ChangedOn)
                .ToList();

            var vm = new PlayerDetailsViewModel
            {
                PlayerId = player.Id,
                Name = playerData.Name,
                MkcId = playerData.MkcId,
                RegistryId = playerData.RegistryId,
                CountryCode = playerData.CountryCode,
                CountryName = playerData.CountryCode == null ? null : _loungeSettingsService.CountryNames.GetValueOrDefault(playerData.CountryCode, null!),
                SwitchFc = playerData.SwitchFc,
                IsHidden = playerData.IsHidden,
                Season = season,
                Mmr = playerData.Mmr,
                MaxMmr = playerData.MaxMmr,
                OverallRank = playerData.IsHidden ? null : playerData.OverallRank,
                MmrChanges = mmrChanges,
                NameHistory = nameHistory,
                RankData = _loungeSettingsService.GetRank(playerData.Mmr, season)!,
                EventsPlayed = playerData.EventsPlayed,
                WinRate = playerData.WinRate,
                WinsLastTen = playerData.LastTenWins,
                LossesLastTen = playerData.LastTenLosses,
                GainLossLastTen = playerData.LastTenGainLoss,
                LargestGain = playerData.LargestGain?.Amount,
                LargestGainTableId = playerData.LargestGain?.EventId,
                LargestLoss = playerData.LargestLoss?.Amount,
                LargestLossTableId = playerData.LargestLoss?.EventId,
                AverageScore = playerData.AverageScore,
                NoSQAverageScore = playerData.NoSQAverageScore,
                AverageLastTen = playerData.AverageLastTen,
                PartnerAverage = playerData.PartnerAverage,
                NoSQPartnerAverage = playerData.NoSQPartnerAverage
            };

            return vm;
        }
    }
}
