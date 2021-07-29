using Lounge.Web.Models;
using Lounge.Web.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lounge.Web.Utils
{
    public static class TableUtils
    {
        private static readonly int[] caps = new[] { 60, 120, 180, 240, -1, 300 };
        private static readonly int[] scalingFactors = new[] { 9500, 5500, 5100, 4800, -1, 4650 };
        private static readonly double[] offsets = new[] { 2746.116, 1589.856, 1474.230, 1387.511, -1, 1344.151 };

        public static string BuildUrl(string tier, (string Player, int Score)[][] scores)
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
                    (string player, int score) = scores[i][j];
                    tableData.Append($"{player} {score}\n");
                }
            }

            string data = Uri.EscapeDataString(tableData.ToString());
            return $"https://gb.hlorenzi.com/table.png?data={data}&loungeapi=true";
        }

        public async static Task<byte[]> GetImageDataAsync(string url)
        {
            using var client = new HttpClient();
            return await client.GetByteArrayAsync(url);
        }

        public static Dictionary<string, int> GetMMRDeltas((string Player, int Score, int CurrentMmr, double Multiplier)[][] scores)
        {
            int numTeams = scores.Length;
            int playersPerTeam = scores[0].Length;

            int[] teamTotalScores = scores.Select(t => t.Sum(s => s.Score)).ToArray();
            int[] places = GetPlaces(teamTotalScores);
            double[] averageMmrs = scores.Select(t => t.Average(s => s.CurrentMmr)).ToArray();

            int cap = caps[playersPerTeam - 1];
            int scalingFactor = scalingFactors[playersPerTeam - 1];
            double offset = offsets[playersPerTeam - 1];

            double GetTeamMmrDeltaWhenWinner(double winnerMmr, double loserMmr)
            {
                return cap / (1 + Math.Pow(11, -(loserMmr - winnerMmr - offset) / scalingFactor));
            }

            double GetTeamMmrDeltaWhenTied(double mmr1, double mmr2)
            {
                return cap / (1 + Math.Pow(11, -(Math.Abs(mmr1 - mmr2) - offset) / scalingFactor)) - cap / 3;
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
                Scores = t.Scores.Select(s => new TableScore
                {
                    Score = s.Score,
                    Multiplier = s.Multiplier,
                    NewMmr = s.NewMmr,
                    PrevMmr = s.PrevMmr,
                    PlayerId = s.PlayerId,
                    Team = s.Team,
                    Player = new Player { Name = s.Player.Name, DiscordId = s.Player.DiscordId },
                }).ToList(),
            });

        public static TableDetailsViewModel GetTableDetails(Table table)
        {
            var teams = new List<TableDetailsViewModel.Team>();

            var sqMultiplier = GetSquadQueueMultiplier(table);

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
                        sqMultiplier * score.Multiplier,
                        score.PrevMmr,
                        score.NewMmr,
                        score.PlayerId,
                        score.Player.Name,
                        score.Player.DiscordId));
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
                createdOn: table.CreatedOn,
                verifiedOn: table.VerifiedOn,
                deletedOn: table.DeletedOn,
                numTeams: table.NumTeams,
                url: url,
                tier: table.Tier,
                teams: teams,
                tableMessageId: table.TableMessageId,
                updateMessageId: table.UpdateMessageId,
                authorId: table.AuthorId);
        }

        public static double GetSquadQueueMultiplier(Table table)
        {
            return string.Equals(table.Tier, "SQ", StringComparison.OrdinalIgnoreCase) ? 0.75 : 1;
        }
    }
}
