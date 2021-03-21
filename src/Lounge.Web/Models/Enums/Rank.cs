using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Models.Enums
{
    public enum Rank
    {
        Grandmaster,
        Master,
        Diamond,
        Sapphire,
        Platinum,
        Gold,
        Silver,
        Bronze,

        [Display(Name = "Iron 2")]
        Iron2,

        [Display(Name = "Iron 1")]
        Iron1,
        Placement
    }
}
