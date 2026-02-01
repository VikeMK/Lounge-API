using Lounge.Web.Data.Entities;
using Lounge.Web.Models.Enums;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Lounge.Web.Utils
{
    public static class TableUtils
    {
        // Given the number of teams in the event, returns how much MMR is gained for beating each opponent if
        // they had equal MMR to you. For example, if your team wins a 4v4v4, you gain 80 MMR for each opponent
        // for a total of 160 MMR.
        private static readonly Dictionary<int, double> GainWhenEqualMmr = new()
        {
            [2] = 100, // Winner = 100 Points
            [3] = 80,  // Winner = 160 Points
            [4] = 60,  // Winner = 180 Points
            [6] = 40,  // Winner = 200 Points
            [8] = 30,  // Winner = 210 Points
            [12] = 20, // Winner = 220 Points
            [24] = 10, // Winner = 230 Points
        };

        // Given the size of a team, returns how much less MMR you need to have than your opponent to gain 1.5x
        // the MMR than what is listed in GainWhenEqualMmr. As the MMR difference approachs infinity, the MMR gain
        // approaches 3x the GainWhenEqualMmr value.
        private static readonly Dictionary<int, double> MmrGapFor150PctGain = new()
        {
            [1] = 2746.116,
            [2] = 1589.856,
            [3] = 1474.230,
            [4] = 1387.511,
            [6] = 1344.151,
            [8] = 1315.245,
            [12] = 1300.792,
        };

        public static string BuildUrl(string tier, (string Player, string? CountryCode, int Score)[][] scores)
        {
            int numTeams = scores.Length;
            int playersPerTeam = scores[0].Length;

            int[] teamTotalScores = scores.Select(t => t.Sum(s => s.Score)).ToArray();
            int[] places = GetPlaces(teamTotalScores);

            var tableData = new StringBuilder();

            string format = playersPerTeam == 1 ? "FFA" : $"{playersPerTeam}v{playersPerTeam}";
            tableData.Append($"#title Tier {tier.ToUpper()} {format}\n");

            if (playersPerTeam == 1)
                tableData.Append("FFA - Free for All #4A82D0\n");

            for (int i = 0; i < numTeams; i++)
            {
                if (playersPerTeam != 1)
                    tableData.Append($"{places[i]} {(i % 2 == 0 ? "#1D6ADE" : "#4A82D0")}\n");

                for (int j = 0; j < playersPerTeam; j++)
                {
                    (string player, string? countryCode, int score) = scores[i][j];
                    tableData.Append($"{player} [{countryCode ?? string.Empty}] {score}\n");
                }
            }

            string data = Uri.EscapeDataString(tableData.ToString());
            return $"https://gb.hlorenzi.com/table.png?data={data}&loungeapi=true";
        }

        public async static Task<byte[]> GetImageDataAsync(string url)
        {
            using var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            using var client = new HttpClient(httpClientHandler);
            return await client.GetByteArrayAsync(url);
        }

        public static Dictionary<string, int> GetMMRDeltas((string Player, int Score, int CurrentMmr, double Multiplier)[][] scores)
        {
            int numTeams = scores.Length;
            int playersPerTeam = scores[0].Length;

            int[] teamTotalScores = scores.Select(t => t.Sum(s => s.Score)).ToArray();
            int[] places = GetPlaces(teamTotalScores);
            double[] averageMmrs = scores.Select(t => t.Average(s => s.CurrentMmr)).ToArray();

            double gainWhenEqualMmr = GainWhenEqualMmr[numTeams];
            double mmrGapFor150PctGain = MmrGapFor150PctGain[playersPerTeam];

            // Max possible MMR gain per opponent with MMR difference approaching infinity
            double cap = gainWhenEqualMmr * 3;

            double GetTeamMmrDeltaWhenWinner(double winnerMmr, double loserMmr)
            {
                double delta = loserMmr - winnerMmr;
                return cap / (1 + Math.Pow(2, 1 - (delta / mmrGapFor150PctGain)));
            }

            double GetTeamMmrDeltaWhenTied(double mmr1, double mmr2)
            {
                double gap = Math.Abs(mmr1 - mmr2);
                return cap / (1 + Math.Pow(2, 1 - (gap / mmrGapFor150PctGain))) - gainWhenEqualMmr;
            }

            int GetTeamMmrDelta(int team)
            {
                double averageMmr = averageMmrs[team];
                int place = places[team];

                double total = 0;
                for (int otherTeam = 0; otherTeam < numTeams; otherTeam++)
                {
                    if (team == otherTeam)
                        continue;

                    double otherAverageMmr = averageMmrs[otherTeam];
                    int otherPlace = places[otherTeam];

                    if (place == otherPlace)
                    {
                        total += (averageMmr < otherAverageMmr ? 1 : -1) * GetTeamMmrDeltaWhenTied(averageMmr, otherAverageMmr);
                    }
                    else if (place < otherPlace)
                    {
                        total += GetTeamMmrDeltaWhenWinner(averageMmr, otherAverageMmr);
                    }
                    else
                    {
                        total -= GetTeamMmrDeltaWhenWinner(otherAverageMmr, averageMmr);
                    }
                }

                return (int)Math.Round(total);
            }

            int[] teamDeltas = Enumerable.Range(0, numTeams).Select(GetTeamMmrDelta).ToArray();

            var deltas = new Dictionary<string, int>();
            for (int i = 0; i < numTeams; i++)
            {
                int teamDelta = teamDeltas[i];
                foreach ((string player, _, int currentMmr, double mult) in scores[i])
                {
                    int delta = teamDelta;

                    // apply any multipliers
                    delta = (int)Math.Round(delta * mult);

                    // ensure the final mmr does not go below 0
                    delta = Math.Max(delta, -currentMmr);

                    deltas[player] = delta;
                }
            }

            return deltas;
        }

        public static int[] GetPlaces(int[] scores)
        {
            int[] placements = new int[scores.Length];
            var sortedTotals = scores
                .Select((score, index) => (Score: score, Index: index))
                .OrderByDescending(t => t.Score);

            int prev = -1;
            int prevPlace = 0;
            int place = 1;
            foreach ((int score, var index) in sortedTotals)
            {
                int curPlace = score == prev ? prevPlace : place;
                placements[index] = curPlace;
                prev = score;
                prevPlace = curPlace;
                place++;
            }

            return placements;
        }

        public static IQueryable<Table> SelectPropertiesForTableDetails(this IQueryable<Table> tables) =>
            tables.Select(t => new Table
            {
                Id = t.Id,
                CreatedOn = t.CreatedOn,
                DeletedOn = t.DeletedOn,
                NumTeams = t.NumTeams,
                Tier = t.Tier,
                TableMessageId = t.TableMessageId,
                UpdateMessageId = t.UpdateMessageId,
                VerifiedOn = t.VerifiedOn,
                AuthorId = t.AuthorId,
                Game = t.Game,
                Season = t.Season,
                Scores = t.Scores.Select(s => new TableScore
                {
                    Score = s.Score,
                    Multiplier = s.Multiplier,
                    NewMmr = s.NewMmr,
                    PrevMmr = t.VerifiedOn == null && t.DeletedOn == null
                        ? s.Player.SeasonData.Where(d => d.Season == t.Season && d.Game == t.Game).Select(d => (int?)d.Mmr).FirstOrDefault()
                        : s.PrevMmr,
                    PlayerId = s.PlayerId,
                    Team = s.Team,
                    Player = new Player { Name = s.Player.Name, DiscordId = s.Player.DiscordId, CountryCode = s.Player.CountryCode },
                }).ToList(),
            });

        public static TableDetailsViewModel GetTableDetails(Table table, ILoungeSettingsService loungeSettingService)
        {
            var teams = new List<TableDetailsViewModel.Team>();

            var formatMultiplier = GetFormatMultiplier(table, loungeSettingService.SquadQueueMultipliers[table.Game][table.Season]);

            int rank = 1;
            int prevTotalScore = 0;
            int prevRank = 1;
            foreach (var team in table.Scores
                .GroupBy(s => s.Team)
                .OrderByDescending(t => t.Sum(t => t.Score)))
            {
                int totalScore = team.Sum(t => t.Score);

                var scores = new List<TableDetailsViewModel.TableScore>();
                foreach (var score in team)
                {
                    scores.Add(new TableDetailsViewModel.TableScore(
                        score.Score,
                        formatMultiplier * score.Multiplier,
                        score.PrevMmr,
                        score.NewMmr,
                        score.PlayerId,
                        score.Player.Name,
                        score.Player.DiscordId,
                        score.Player.CountryCode));
                }

                int actualRank = totalScore == prevTotalScore ? prevRank : rank;
                teams.Add(new TableDetailsViewModel.Team(rank: actualRank, scores: scores));

                prevRank = actualRank;
                prevTotalScore = totalScore;
                rank++;
            }

            var url = $"/TableImage/{table.Id}.png";

            return new TableDetailsViewModel(
                id: table.Id,
                game: table.Game,
                season: table.Season, 
                createdOn: table.CreatedOn,
                verifiedOn: table.VerifiedOn,
                deletedOn: table.DeletedOn,
                numTeams: table.NumTeams,
                numPlayers: teams.Sum(t => t.Scores.Count),
                url: url,
                tier: table.Tier,
                teams: teams,
                tableMessageId: table.TableMessageId,
                updateMessageId: table.UpdateMessageId,
                authorId: table.AuthorId);
        }

        public static double GetFormatMultiplier(Table table, double sqMultiplier)
        {
            return string.Equals(table.Tier, "SQ", StringComparison.OrdinalIgnoreCase) ? sqMultiplier : 1;
        }

        public static string TierDisplayName(string? tierName) =>
            tierName?.ToUpperInvariant() switch
            {
                "SQ" => "Squad Queue",
                string tier => $"Tier {tier}",
                null => "Table",
            };

        public static string? FormatDisplay(int numTeams, int numPlayers)
        {
            if (numTeams == 0)
                return null;

            var playersPerTeam = numPlayers / numTeams;
            return playersPerTeam switch
            {
                1 => "FFA",
                2 or 3 or 4 or 6 or 8 or 12 => $"{playersPerTeam}v{playersPerTeam}",
                int n => null,
            };
        }
    }
}
