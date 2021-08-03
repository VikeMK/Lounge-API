using System.Collections.Generic;

namespace Lounge.Web.Models.ViewModels
{
    public class PlayerListViewModel
    {
        public List<Player> Players { get; init; }

        public record Player(string Name, int MKCId, int? Mmr, string? DiscordId, int EventsPlayed);
    }
}
