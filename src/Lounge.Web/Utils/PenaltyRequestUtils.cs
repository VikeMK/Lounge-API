using Lounge.Web.Data.Entities;
using Lounge.Web.Models.Enums;
using Lounge.Web.Models.ViewModels;

namespace Lounge.Web.Utils
{
    public static class PenaltyRequestUtils
    {
        public static PenaltyRequestViewModel GetPenaltyRequestDetails(PenaltyRequest request, string playerName, string reporterName)
        {
            return new PenaltyRequestViewModel
            {
                Id = request.Id,
                Game = (Game)request.Game,
                PenaltyName = request.PenaltyName,
                TableId = request.TableId,
                NumberOfRaces = request.NumberOfRaces,
                ReporterId = request.ReporterId ?? default,
                ReporterName = reporterName,
                PlayerId = request.PlayerId,
                PlayerName = playerName
            };
        }
    }
}
