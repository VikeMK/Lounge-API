using Lounge.Web.Models.Enums;
using System;

namespace Lounge.Web.Models.ViewModels
{
    public class PenaltyViewModel
    {
        public int Id { get; init; }
        public Game Game { get; init; }
        public int Season { get; init; }
        public DateTime AwardedOn { get; init; }
        public bool IsStrike { get; init; }
        public int PrevMmr { get; init; }
        public int NewMmr { get; init; }
        public int Amount => PrevMmr - NewMmr;
        public DateTime? DeletedOn { get; init; }
        public int PlayerId { get; init; }
        public required string PlayerName { get; init; }
    }
}
