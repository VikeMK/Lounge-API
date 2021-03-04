using Lounge.Web.Models.Enums;
using System;

namespace Lounge.Web.Utils
{
    public static class RankUtils
    {
        public static Rank GetRank(int? mmr) => mmr switch
        {
            >= 14500 => Rank.Grandmaster,
            >= 13000 => Rank.Master,
            >= 11500 => Rank.Diamond,
            >= 10000 => Rank.Sapphire,
            >= 8500 => Rank.Platinum,
            >= 7000 => Rank.Gold,
            >= 5500 => Rank.Silver,
            >= 4000 => Rank.Bronze,
            >= 2000 => Rank.Iron2,
            >= 0 => Rank.Iron1,
            null => Rank.Placement,
            _ => throw new ArgumentException("mmr must be a non-negative integer", nameof(mmr)),
        };
    }
}
