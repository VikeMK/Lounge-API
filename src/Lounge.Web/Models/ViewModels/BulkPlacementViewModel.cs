using Lounge.Web.Models.Enums;
using System.Collections.Generic;

namespace Lounge.Web.Models.ViewModels;

public class BulkPlacementViewModel
{
    public Game Game { get; set; } = Game.mk8dx;
    public List<PlayerPlacementViewModel> PlayerPlacements { get; set; } = new();

    public class PlayerPlacementViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int Mmr { get; set; }
    }
}