using System.Collections.Generic;

namespace Lounge.Web.Models.ViewModels
{
    public class PlayerListViewModel
    {
        public required List<Player> Players { get; init; }

        public record Player(string Name, int MKCId, int? Mmr, string? DiscordId, int EventsPlayed);
    }
}
