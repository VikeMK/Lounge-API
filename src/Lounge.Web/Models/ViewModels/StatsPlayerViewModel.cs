using System.Collections.Generic;

namespace Lounge.Web.Models.ViewModels
{
    public class StatsPlayerViewModel
    {
        public required List<Player> Players { get; init; }

        public record Player(string Name, int? Mmr, string? CountryCode);

    }
}