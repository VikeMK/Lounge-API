using System;

namespace Lounge.Web.Models.ViewModels
{
    public class BonusViewModel
    {
        public int Id { get; init; }
        public int Season { get; init; }
        public DateTime AwardedOn { get; init; }
        public int PrevMmr { get; init; }
        public int NewMmr { get; init; }
        public int Amount => NewMmr - PrevMmr;
        public DateTime? DeletedOn { get; init; }
        public int PlayerId { get; init; }
        public string PlayerName { get; init; }
    }
}
