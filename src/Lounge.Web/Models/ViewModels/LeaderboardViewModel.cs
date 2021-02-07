using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Models.ViewModels
{
    public class LeaderboardViewModel
    {
        public List<Player> Players { get; set; }

        public class Player
        {
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
