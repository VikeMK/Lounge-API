using System;
using System.Collections.Generic;

namespace Lounge.Web.Models.ViewModels
{
    public class NewTableViewModel
    {
        public NewTableViewModel(string tier, List<PlayerScore> scores)
        {
            Tier = tier ?? throw new ArgumentNullException(nameof(tier));
            Scores = scores ?? throw new ArgumentNullException(nameof(scores));
        }

        public string Tier { get; set; }
        public List<PlayerScore> Scores { get; set; }
        public string? AuthorId { get; set; }

        public class PlayerScore
        {
            public PlayerScore(string playerName, int team, int score, double multiplier = 1)
            {
                PlayerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
                Team = team;
                Score = score;
                Multiplier = multiplier;
            }

            public string PlayerName { get; set; }
            public int Team { get; set; }
            public int Score { get; set; }
            public double Multiplier { get; set; } = 1;
        }
    }
}
