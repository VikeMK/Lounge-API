using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Stats
{
    public enum LeaderboardSortOrder
    {
        Name,

        [Display(Name = "MMR")]
        Mmr,

        [Display(Name = "Peak MMR")]
        MaxMmr,

        [Display(Name = "Events Played")]
        EventsPlayed,

        [Display(Name = "Average Score (No SQ)")]
        AverageScoreNoSQ,

        [Display(Name = "Win Rate")]
        WinRate,

        [Display(Name = "Win - Loss (Last 10)")]
        WinLossLast10,

        [Display(Name = "Gain/Loss (Last 10)")]
        GainLast10,

        [Display(Name = "Average Score (No SQ, Last 10)")]
        AverageScoreNoSQLast10
    }
}
