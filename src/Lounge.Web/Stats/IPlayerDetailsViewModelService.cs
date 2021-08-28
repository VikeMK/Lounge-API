using Lounge.Web.Models.ViewModels;

namespace Lounge.Web.Stats
{
    public interface IPlayerDetailsViewModelService
    {
        PlayerDetailsViewModel? GetPlayerDetails(int playerId, int season);
    }
}