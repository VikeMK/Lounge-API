using Lounge.Web.Data.Entities;
using Lounge.Web.Models.Enums;
using Lounge.Web.Models.ViewModels;

namespace Lounge.Web.Utils
{
    public static class BonusUtils
    {
        public static BonusViewModel GetBonusDetails(Bonus bonus, string playerName)
        {
            return new BonusViewModel
            {
                Id = bonus.Id,
                Game = bonus.Game,
                Season = bonus.Season,
                AwardedOn = bonus.AwardedOn,
                DeletedOn = bonus.DeletedOn,
                PrevMmr = bonus.PrevMmr,
                NewMmr = bonus.NewMmr,
                PlayerId = bonus.PlayerId,
                PlayerName = playerName
            };
        }
    }
}
