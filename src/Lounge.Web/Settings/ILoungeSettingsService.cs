using Lounge.Web.Models.Enums;
using Lounge.Web.Models.ViewModels;
using System;
using System.Collections.Generic;

namespace Lounge.Web.Settings
{
    public interface ILoungeSettingsService
    {
        IReadOnlyList<Game> ValidGames { get; }
        IReadOnlyDictionary<Game, int> CurrentSeason { get; }
        IReadOnlyDictionary<Game, IReadOnlyDictionary<int, TimeSpan>> LeaderboardRefreshDelays { get; }
        IReadOnlyDictionary<Game, IReadOnlyList<int>> ValidSeasons { get; }
        IReadOnlyDictionary<Game, IReadOnlyDictionary<int, double>> SquadQueueMultipliers { get; }
        IReadOnlyDictionary<string, string> CountryNames { get; }
        IReadOnlyDictionary<Game, IReadOnlyDictionary<int, IReadOnlyList<string>>> RecordsTierOrders { get; }

        Rank? GetRank(int? mmr, Game game, int season);
    }
}