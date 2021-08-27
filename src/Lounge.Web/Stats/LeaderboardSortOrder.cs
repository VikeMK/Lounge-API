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

        [Display(Name = "Win Rate")]
        WinRate,

        [Display(Name = "Win\u00a0-\u00a0Loss (Last\u00a010)")]
        WinLossLast10, 

        [Display(Name = "Gain/Loss (Last\u00a010)")]
        GainLast10,

        [Display(Name = "Events Played")]
        EventsPlayed,

        [Display(Name = "Largest Gain")]
        LargestGain,

        [Display(Name = "Largest Loss")]
        LargestLoss
    }
}
