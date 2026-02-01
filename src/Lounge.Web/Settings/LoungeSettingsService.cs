using Lounge.Web.Models.Enums;
using Lounge.Web.Models.ViewModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Lounge.Web.Settings
{
    public class LoungeSettingsService : ILoungeSettingsService
    {
        private Lazy<ParsedLoungeSettings> _settings;
        private static readonly IReadOnlyList<GameMode> _validGames = [GameMode.mk8dx, GameMode.mkworld, GameMode.mkworld12p, GameMode.mkworld24p];

        public LoungeSettingsService(IOptionsMonitor<LoungeSettings> optionsMonitor)
        {
            _settings = new Lazy<ParsedLoungeSettings>(() => ParsedLoungeSettings.Create(optionsMonitor.CurrentValue));
            optionsMonitor.OnChange((settings) => _settings = new Lazy<ParsedLoungeSettings>(() => ParsedLoungeSettings.Create(optionsMonitor.CurrentValue)));
        }

        public IReadOnlyList<GameMode> ValidGames => _validGames;

        public bool TryGetCurrentSeason(GameMode gameMode, [NotNullWhen(true)] out int? season)
        {
            if (_settings.Value.CurrentSeason.TryGetValue(gameMode, out var s))
            {
                season = s;
                return true;
            }
            else
            {
                season = null;
                return false;
            }
        }

        public IReadOnlyDictionary<GameMode, IReadOnlyList<int>> ValidSeasons => _settings.Value.ValidSeasons;

        public IReadOnlyDictionary<string, string> CountryNames => _settings.Value.CountryNames;

        public IReadOnlyDictionary<GameMode, IReadOnlyDictionary<int, TimeSpan>> LeaderboardRefreshDelays => _settings.Value.LeaderboardRefreshDelays;

        public IReadOnlyDictionary<GameMode, IReadOnlyDictionary<int, double>> SquadQueueMultipliers => _settings.Value.SquadQueueMultipliers;

        public IReadOnlyDictionary<GameMode, IReadOnlyDictionary<int, IReadOnlyList<string>>> RecordsTierOrders => _settings.Value.RecordsTierOrders;
        
        public Rank GetRank(int? mmr, GameMode game, int season)
        {
            if (!_settings.Value.MmrRanks[game].TryGetValue(season, out var mmrRanks))
                throw new Exception($"No MMR ranks configured for game {game} season {season}");

            if (mmr is null)
                return new Rank(Division.Placement);

            Rank? prev = null;
            foreach ((int rankMmr, Rank rank) in mmrRanks)
            {
                if (mmr >= rankMmr)
                    prev = rank;
                else
                    break;
            }

            return prev ?? throw new Exception($"No rank found for MMR {mmr} in game {game} season {season}");
        }

        public IReadOnlyDictionary<string, int> GetRanks(GameMode game, int season)
        {
            if (_settings.Value.RanksByName[game].TryGetValue(season, out var ranks))
            {
                return ranks;
            }
            return new Dictionary<string, int>();
        }

        public IReadOnlyList<string> GetRecordsTierOrder(GameMode game, int season)
        {
            if (_settings.Value.RecordsTierOrders[game].TryGetValue(season, out var tierOrder))
            {
                return tierOrder;
            }
            return Array.Empty<string>();
        }

        public IReadOnlyDictionary<string, IReadOnlyList<string>> GetDivisionsToTier(GameMode game, int season)
        {
            if (_settings.Value.DivisionsToTier[game].TryGetValue(season, out var divisionsToTier))
            {
                return divisionsToTier;
            }
            return new Dictionary<string, IReadOnlyList<string>>();
        }
        
        record ParsedLoungeSettings(
            IReadOnlyDictionary<GameMode, int> CurrentSeason,
            IReadOnlyDictionary<GameMode, IReadOnlyList<int>> ValidSeasons,
            IReadOnlyDictionary<GameMode, IReadOnlyDictionary<int, TimeSpan>> LeaderboardRefreshDelays,
            IReadOnlyDictionary<GameMode, IReadOnlyDictionary<int, double>> SquadQueueMultipliers,
            IReadOnlyDictionary<GameMode, IReadOnlyDictionary<int, IReadOnlyList<(int Mmr, Rank Rank)>>> MmrRanks,
            IReadOnlyDictionary<string, string> CountryNames,
            IReadOnlyDictionary<GameMode, IReadOnlyDictionary<int, IReadOnlyList<string>>> RecordsTierOrders,
            IReadOnlyDictionary<GameMode, IReadOnlyDictionary<int, IReadOnlyDictionary<string, int>>> RanksByName,
            IReadOnlyDictionary<GameMode, IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<string>>>> DivisionsToTier)
        {
            public static ParsedLoungeSettings Create(LoungeSettings loungeSettings)
            {
                var currentSeason = new Dictionary<GameMode, int>();
                var allValidSeasons = new Dictionary<GameMode, IReadOnlyList<int>>();
                var allRefreshDelays = new Dictionary<GameMode, IReadOnlyDictionary<int, TimeSpan>>();
                var allSqMultipliers = new Dictionary<GameMode, IReadOnlyDictionary<int, double>>();
                var allMmrRanks = new Dictionary<GameMode, IReadOnlyDictionary<int, IReadOnlyList<(int Mmr, Rank Rank)>>>();
                var allRecordsTierOrders = new Dictionary<GameMode, IReadOnlyDictionary<int, IReadOnlyList<string>>>();
                var allRanksByName = new Dictionary<GameMode, IReadOnlyDictionary<int, IReadOnlyDictionary<string, int>>>();
                var allDivisionsToTier = new Dictionary<GameMode, IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<string>>>>();

                foreach ((var gameStr, var gameSettings) in loungeSettings.Games)
                {
                    var game = Enum.Parse<GameMode>(gameStr, true);

                    var validSeasons = new List<int>();
                    var refreshDelays = new Dictionary<int, TimeSpan>();
                    var sqMultipliers = new Dictionary<int, double>();
                    var mmrRanks = new Dictionary<int, IReadOnlyList<(int Mmr, Rank Rank)>>();
                    var recordsTierOrders = new Dictionary<int, IReadOnlyList<string>>();
                    var ranksByName = new Dictionary<int, IReadOnlyDictionary<string, int>>();
                    var divisionsToTier = new Dictionary<int, IReadOnlyDictionary<string, IReadOnlyList<string>>>();

                    foreach ((var seasonStr, var settings) in gameSettings.Seasons)
                    {
                        if (int.TryParse(seasonStr, out int season))
                        {
                            validSeasons.Add(season);
                            refreshDelays[season] = settings.LeaderboardRefreshDelay;
                            sqMultipliers[season] = settings.SquadQueueMultiplier;
                            recordsTierOrders[season] = settings.RecordsTierOrder;
                            ranksByName[season] = settings.Ranks;
                            divisionsToTier[season] = settings.DivisionsToTier;

                            var seasonRanks = new List<(int Mmr, Rank Rank)>();

                            foreach ((string rank, int mmr) in settings.Ranks)
                            {
                                string divisionStr = rank;
                                int? level = null;
                                if (rank.Contains(' '))
                                {
                                    var rankParts = rank.Split(' ');
                                    divisionStr = rankParts[0];
                                    level = int.Parse(rankParts[1]);
                                }

                                var division = Enum.Parse<Division>(divisionStr);
                                seasonRanks.Add((mmr, new Rank(division, level)));
                            }

                            seasonRanks = seasonRanks.OrderBy(r => r.Mmr).ToList();
                            mmrRanks[season] = seasonRanks;
                        }
                    }

                    if (gameSettings.CurrentSeason is int currentSeasonValue)
                        currentSeason[game] = currentSeasonValue;
                    allValidSeasons[game] = validSeasons;
                    allRefreshDelays[game] = refreshDelays;
                    allSqMultipliers[game] = sqMultipliers;
                    allMmrRanks[game] = mmrRanks;
                    allRecordsTierOrders[game] = recordsTierOrders;
                    allRanksByName[game] = ranksByName;
                    allDivisionsToTier[game] = divisionsToTier;
                }


                return new(currentSeason, allValidSeasons, allRefreshDelays, allSqMultipliers, allMmrRanks, loungeSettings.CountryNames, allRecordsTierOrders, allRanksByName, allDivisionsToTier);
            }
        }
    }
}
