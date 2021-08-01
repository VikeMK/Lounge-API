using Lounge.Web.Models;
using Lounge.Web.Models.ViewModels;

namespace Lounge.Web.Utils
{
    public static class PenaltyUtils
    {
        public static PenaltyViewModel GetPenaltyDetails(Penalty penalty, string playerName)
        {
            return new PenaltyViewModel
            {
                Id = penalty.Id,
                Season = penalty.Season,
                AwardedOn = penalty.AwardedOn,
                DeletedOn = penalty.DeletedOn,
                IsStrike = penalty.IsStrike,
                PrevMmr = penalty.PrevMmr,
                NewMmr = penalty.NewMmr,
                PlayerId = penalty.PlayerId,
                PlayerName = playerName
            };
        }
    }
}
