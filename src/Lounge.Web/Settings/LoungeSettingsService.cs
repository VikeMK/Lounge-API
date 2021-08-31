using Lounge.Web.Models.Enums;
using Lounge.Web.Models.ViewModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lounge.Web.Settings
{
    public class LoungeSettingsService : ILoungeSettingsService
    {
        private Lazy<ParsedLoungeSettings> _settings;

        public LoungeSettingsService(IOptionsMonitor<LoungeSettings> optionsMonitor)
        {
            _settings = new Lazy<ParsedLoungeSettings>(() => ParsedLoungeSettings.Create(optionsMonitor.CurrentValue));
            optionsMonitor.OnChange((settings) => _settings = new Lazy<ParsedLoungeSettings>(() => ParsedLoungeSettings.Create(optionsMonitor.CurrentValue)));
        }

        public int CurrentSeason => _settings.Value.CurrentSeason;

        public IReadOnlyList<int> ValidSeasons => _settings.Value.ValidSeasons;

        public IReadOnlyDictionary<string, string> CountryNames => _settings.Value.CountryNames;

        public IReadOnlyDictionary<int, TimeSpan> LeaderboardRefreshDelays => _settings.Value.LeaderboardRefreshDelays;

        public IReadOnlyDictionary<int, double> SquadQueueMultipliers => _settings.Value.SquadQueueMultipliers;

        public IReadOnlyDictionary<int, IReadOnlyList<string>> RecordsTierOrders => _settings.Value.RecordsTierOrders;

        public Rank? GetRank(int? mmr, int season)
        {
            if (_settings.Value.MmrRanks.TryGetValue(season, out var mmrRanks))
            {
                if (mmr is null)
                    return new Rank(Division.Placement);

                Rank? prev = null;
                foreach ((int rankMmr, Rank rank) in mmrRanks)
                {
                    if (mmr >= rankMmr)
                        prev = rank;
                    else
                        return prev;
                }

                return prev;
            }

            return null;
        }

        record ParsedLoungeSettings(
            int CurrentSeason,
            IReadOnlyList<int> ValidSeasons,
            IReadOnlyDictionary<int, TimeSpan> LeaderboardRefreshDelays,
            IReadOnlyDictionary<int, double> SquadQueueMultipliers,
            IReadOnlyDictionary<int, IReadOnlyList<(int Mmr, Rank Rank)>> MmrRanks,
            IReadOnlyDictionary<string, string> CountryNames,
            IReadOnlyDictionary<int, IReadOnlyList<string>> RecordsTierOrders)
        {
            public static  ParsedLoungeSettings Create(LoungeSettings loungeSettings)
            {
                var validSeasons = new List<int>();
                var refreshDelays = new Dictionary<int, TimeSpan>();
                var sqMultipliers = new Dictionary<int, double>();
                var mmrRanks = new Dictionary<int, IReadOnlyList<(int Mmr, Rank Rank)>>();
                var recordsTierOrders = new Dictionary<int, IReadOnlyList<string>>();

                foreach ((var seasonStr, var settings) in loungeSettings.Seasons)
                {
                    if (int.TryParse(seasonStr, out int season))
                    {
                        validSeasons.Add(season);
                        refreshDelays[season] = settings.LeaderboardRefreshDelay;
                        sqMultipliers[season] = settings.SquadQueueMultiplier;
                        recordsTierOrders[season] = settings.RecordsTierOrder;

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

                return new(loungeSettings.CurrentSeason, validSeasons, refreshDelays, sqMultipliers, mmrRanks, loungeSettings.CountryNames, recordsTierOrders);
            }
        }
    }
}
