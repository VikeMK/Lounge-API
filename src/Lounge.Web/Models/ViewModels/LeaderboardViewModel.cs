using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Models.ViewModels
{
    public class LeaderboardViewModel
    {
        public LeaderboardViewModel(List<Player> players)
        {
            Players = players;
        }

        public List<Player> Players { get; set; }

        public class Player
        {
            public Player(int id, string name, int? mmr, int? maxMmr)
            {
                Id = id;
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Mmr = mmr;
                MaxMmr = maxMmr;
            }

            public int Id { get; set; }

            public string Name { get; set; }

            [Display(Name = "MMR")]
            [DisplayFormat(NullDisplayText = "Placement")]
            public int? Mmr { get; set; }

            [Display(Name = "Peak MMR")]
            [DisplayFormat(NullDisplayText = "Placement")]
            public int? MaxMmr { get; set; }
        }
    }
}
