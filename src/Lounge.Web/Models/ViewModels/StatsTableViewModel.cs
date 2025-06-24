using System;
using System.Collections.Generic;

namespace Lounge.Web.Models.ViewModels
{
    public class StatsTableViewModel
    {
        public required List<Table> Tables { get; init; }

        public record Table(DateTime CreatedOn, int NumTeams, string Tier, int NumPlayers);
    }
}