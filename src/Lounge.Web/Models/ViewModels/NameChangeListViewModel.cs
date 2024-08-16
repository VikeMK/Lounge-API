using System;
using System.Collections.Generic;

namespace Lounge.Web.Models.ViewModels
{
    public class NameChangeListViewModel
    {
        public required List<Player> Players { get; init; }

        public record Player(int Id, string? DiscordId, string Name, string NewName, DateTime RequestedOn, string? MessageId);
    }
}
