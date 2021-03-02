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
                    time: tableScore.Table.VerifiedOn!.Value));

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
                    reason: PlayerDetailsViewModel.MmrChangeReason.Penalty,
                    time: penalty.AwardedOn));

                if (penalty.DeletedOn is not null)
                {
                    mmrChanges.Add(new PlayerDetailsViewModel.MmrChange(
                        changeId: penalty.Id,
                        newMmr: -1,
                        mmrDelta: -delta,
                        reason: PlayerDetailsViewModel.MmrChangeReason.PenaltyDelete,
                        time: penalty.DeletedOn.Value));
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
