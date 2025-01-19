using System.Collections.Generic;

public class BulkPlacementViewModel
{
    public List<PlayerPlacementViewModel> PlayerPlacements { get; set; } = new();

    public class PlayerPlacementViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int Mmr { get; set; }
    }
}

