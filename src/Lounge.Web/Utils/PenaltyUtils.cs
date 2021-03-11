using Lounge.Web.Models;
using Lounge.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lounge.Web.Utils
{
    public static class PenaltyUtils
    {
        public static PenaltyViewModel GetPenaltyDetails(Penalty penalty)
        {
            return new PenaltyViewModel
            {
                Id = penalty.Id,
                AwardedOn = penalty.AwardedOn,
                DeletedOn = penalty.DeletedOn,
                IsStrike = penalty.IsStrike,
                PrevMmr = penalty.PrevMmr,
                NewMmr = penalty.NewMmr,
                PlayerId = penalty.PlayerId,
                PlayerName = penalty.Player.Name
            };
        }
    }
}
