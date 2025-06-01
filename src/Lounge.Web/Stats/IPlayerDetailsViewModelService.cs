using Lounge.Web.Models.Enums;
using Lounge.Web.Models.ViewModels;

namespace Lounge.Web.Stats
{
    public interface IPlayerDetailsViewModelService
    {
        PlayerDetailsViewModel? GetPlayerDetails(int playerId, Game game, int season);
    }
}