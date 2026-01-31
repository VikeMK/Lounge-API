using Lounge.Web.Models.Enums;
using System.Collections.Generic;

namespace Lounge.Web.Models.ViewModels
{
    public class PlayerListViewModel
    {
        public required GameMode Game { get; init; }
        public required int Season { get; init; }
        public required List<Player> Players { get; init; }

        public record Player(string Name, int Id, int MKCId, int? Mmr, string? DiscordId, int EventsPlayed);
    }
}
