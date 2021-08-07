using Lounge.Web.Models.ViewModels;
using System;
using System.Collections.Generic;

namespace Lounge.Web.Settings
{
    public interface ILoungeSettingsService
    {
        int CurrentSeason { get; }
        IReadOnlyDictionary<int, TimeSpan> LeaderboardRefreshDelays { get; }
        IReadOnlyList<int> ValidSeasons { get; }

        Rank? GetRank(int? mmr, int season);
    }
}