using Lounge.Web.Models;
using Lounge.Web.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lounge.Web.Utils
{
    public static class PlayerUtils
    {
        public static string NormalizeName(string name) => string.Join("", name.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

        public static IQueryable<Player> SelectPropertiesForPlayerDetails(this IQueryable<Player> players) =>
            players.Select(p => new Player
            {
                Id = p.Id,
                InitialMmr = p.InitialMmr,
                MaxMmr = p.MaxMmr,
                MKCId = p.MKCId,
                Mmr = p.Mmr,
                Name = p.Name,
                PlacedOn = p.PlacedOn,
                NormalizedName = p.NormalizedName,
                Bonuses = p.Bonuses.Select(b => new Bonus { Id = b.Id, AwardedOn = b.AwardedOn, DeletedOn = b.DeletedOn, NewMmr = b.NewMmr, PrevMmr = b.PrevMmr }).ToList(),
                Penalties = p.Penalties.Select(pen => new Penalty { Id = pen.Id, AwardedOn = pen.AwardedOn, DeletedOn = pen.DeletedOn, NewMmr = pen.NewMmr, PrevMmr = pen.PrevMmr }).ToList(),
                TableScores = p.TableScores.Select(t => new TableScore
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
                        Scores = t.Table.Scores.Select(s => new TableScore { Score = s.Score, Team = s.Team, PlayerId = s.PlayerId }).ToList()
                    }
                }).ToList(),
            });

        public static PlayerDetailsViewModel GetPlayerDetails(Player player)
        {
            var mmrChanges = new List<PlayerDetailsViewModel.MmrChange>();
            if (player.InitialMmr is not null)
            {
                mmrChanges.Add(new PlayerDetailsViewModel.MmrChange(
                    changeId: null,
                    newMmr: player.InitialMmr.Value,
                    mmrDelta: 0,
                    reason: PlayerDetailsViewModel.MmrChangeReason.Placement,
                    time: player.PlacedOn!.Value));
            }

            foreach (var tableScore in player.TableScores)
            {
                if (tableScore.Table.VerifiedOn is null)
                    continue;

                var newMmr = tableScore.NewMmr!.Value;
                var delta = newMmr - tableScore.PrevMmr!.Value;

                mmrChanges.Add(new PlayerDetailsViewModel.MmrChange(
                    changeId: tableScore.TableId,
                    newMmr: newMmr,
                    mmrDelta: delta,
                    reason: PlayerDetailsViewModel.MmrChangeReason.Table,
                    time: tableScore.Table.VerifiedOn!.Value,
                    score: tableScore.Score,
                    partnerScores: tableScore.Table.Scores
                        .Where(s => s.Team == tableScore.Team && s.PlayerId != player.Id)
                        .Select(s => s.Score)
                        .ToList())) ;

                if (tableScore.Table.DeletedOn is not null)
                {
                    mmrChanges.Add(new PlayerDetailsViewModel.MmrChange(
                        changeId: tableScore.TableId,
                        newMmr: -1,
                        mmrDelta: -delta,
                        reason: PlayerDetailsViewModel.MmrChangeReason.TableDelete,
                        time: tableScore.Table.DeletedOn!.Value));
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

            // sort descending
            mmrChanges.Reverse();

            var vm = new PlayerDetailsViewModel(
                playerId: player.Id,
                name: player.Name,
                mkcId: player.MKCId,
                mmr: player.Mmr,
                maxMmr: player.MaxMmr,
                mmrChanges: mmrChanges);

            return vm;
        }
    }
}
