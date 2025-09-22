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

        [Display(Name = "Last Week")]
        LastWeekRankChange,

        [Display(Name = "Events")]
        EventsPlayed,

        [Display(Name = "Average Score (12P)")]
        AvgScore12P,

        [Display(Name = "Average Score (24P)")]
        AvgScore24P
    }
}
