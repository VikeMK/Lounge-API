using Lounge.Web.Models.Enums;
using Lounge.Web.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Lounge.Web.Settings
{    
    public interface ILoungeSettingsService
    {
        IReadOnlyList<GameMode> ValidGames { get; }
        bool TryGetCurrentSeason(GameMode gameMode, [NotNullWhen(true)] out int? season);
        IReadOnlyDictionary<GameMode, IReadOnlyDictionary<int, TimeSpan>> LeaderboardRefreshDelays { get; }
        IReadOnlyDictionary<GameMode, IReadOnlyList<int>> ValidSeasons { get; }
        IReadOnlyDictionary<GameMode, IReadOnlyDictionary<int, double>> SquadQueueMultipliers { get; }
        IReadOnlyDictionary<string, string> CountryNames { get; }
        IReadOnlyDictionary<GameMode, IReadOnlyDictionary<int, IReadOnlyList<string>>> RecordsTierOrders { get; }

        Rank GetRank(int? mmr, GameMode game, int season);
        IReadOnlyDictionary<string, int> GetRanks(GameMode game, int season);
        IReadOnlyList<string> GetRecordsTierOrder(GameMode game, int season);
        IReadOnlyDictionary<string, IReadOnlyList<string>> GetDivisionsToTier(GameMode game, int season);

        bool ValidateCurrentGame(ref GameMode game, [NotNullWhen(true)] out int? currentSeason, [NotNullWhen(false)] out string? error, bool allowMkWorldFallback)
        {
            if (!ValidGames.Contains(game))
            {
                currentSeason = null;
                error = $"Invalid game {game}";
                return false;
            }

            if (allowMkWorldFallback && game == GameMode.mkworld)
                game = GameMode.mkworld24p;

            if (!TryGetCurrentSeason(game, out currentSeason))
            {
                error = $"Game {game} has no current season";
                return false;
            }

            error = null;
            return true;
        }

        bool ValidateGameMatchesAndFromCurrentSeason(GameMode game, int season, GameMode actualGame, [NotNullWhen(false)] out string? error)
        {
            var matches = (game == actualGame) || (game == GameMode.mkworld && (actualGame is GameMode.mkworld12p or GameMode.mkworld24p));
            if (!matches)
            {
                error = $"Game {game} does not match actual game {actualGame}";
                return false;
            }

            if (!TryGetCurrentSeason(actualGame, out var currentSeason) || season != currentSeason)
            {
                error = $"Attempted to perform operation on data from a previous season {season}";
                return false;
            }

            error = null;
            return true;
        }

        bool ValidateGameAndSeason(ref GameMode game, [NotNullWhen(true)] ref int? season, [NotNullWhen(false)] out string? error, bool allowMkWorldFallback)
        {
            if (!ValidGames.Contains(game))
            {
                error = $"Invalid game {game}";
                return false;
            }

            if (season is null)
            {
                if (allowMkWorldFallback && game == GameMode.mkworld)
                    game = GameMode.mkworld24p;

                if (!TryGetCurrentSeason(game, out season))
                {
                    error = $"Game {game} has no current season";
                    return false;
                }

                error = null;
                return true;
            }

            if (allowMkWorldFallback)
            {
                switch (game)
                {
                    case GameMode.mkworld12p or GameMode.mkworld24p when season is 0 or 1:
                        game = GameMode.mkworld;
                        break;
                    case GameMode.mkworld when season is >= 2:
                        game = GameMode.mkworld24p;
                        break;
                }
            }

            var validSeasons = ValidSeasons[game];
            if (!validSeasons.Contains(season.Value))
            {
                error = $"Invalid season {season} for game {game}";
                return false;
            }

            error = null;
            return true;
        }
    }
}