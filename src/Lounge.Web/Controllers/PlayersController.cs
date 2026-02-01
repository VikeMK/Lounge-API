using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Lounge.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Lounge.Web.Models.ViewModels;
using Lounge.Web.Utils;
using System.Linq;
using Lounge.Web.Stats;
using System.Collections.Generic;
using Lounge.Web.Settings;
using Lounge.Web.Data.Entities;
using Lounge.Web.Data.ChangeTracking;
using Lounge.Web.Models.Enums;
using Lounge.Web.MkcRegistry;
using System.Diagnostics;

namespace Lounge.Web.Controllers
{
    [Route("api/player")]
    [Authorize]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPlayerStatCache _playerStatCache;
        private readonly IPlayerDetailsCache _playerDetailsCache;
        private readonly IPlayerDetailsViewModelService _playerDetailsViewModelService;
        private readonly IDbCache _dbCache;
        private readonly ILoungeSettingsService _loungeSettingsService;
        private readonly IMkcRegistryApi _mkcRegistryApi;

        public PlayersController(ApplicationDbContext context, IPlayerDetailsViewModelService playerDetailsViewModelService, IPlayerDetailsCache playerDetailsCache, IPlayerStatCache playerStatCache, IDbCache dbCache, ILoungeSettingsService loungeSettingsService, IMkcRegistryApi mkcRegistryApi)
        {
            _context = context;
            _playerStatCache = playerStatCache;
            _loungeSettingsService = loungeSettingsService;
            _mkcRegistryApi = mkcRegistryApi;
            _playerDetailsViewModelService = playerDetailsViewModelService;
            _playerDetailsCache = playerDetailsCache;
            _dbCache = dbCache;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PlayerGameViewModel>> GetPlayer(string? name, int? id, int? mkcId, string? discordId, string? fc, GameMode game = GameMode.mk8dx, int? season = null)
        {
            if (!_loungeSettingsService.ValidateGameAndSeason(ref game, ref season, out var error, allowMkWorldFallback: true))
                return BadRequest(error);

            Player? player;
            if (id is not null)
            {
                player = await GetGamePlayerByIdAsync(id.Value, game);
            }
            else if (name is not null)
            {
                player = await GetGamePlayerByNameAsync(name, game);
            }
            else if (mkcId is not null)
            {
                player = await GetGamePlayerByRegistryIdAsync(mkcId.Value, game);
            }
            else if (discordId is not null)
            {
                player = await GetGamePlayerByDiscordIdAsync(discordId, game);
            }
            else if (fc is not null)
            {
                player = await GetGamePlayerByFriendCodeAsync(fc, game);
            }
            else
            {
                return BadRequest("Must provide name, MKC ID, or discord ID");
            }

            if (player is null)
                return NotFound();

            var seasonData = player.SeasonData.FirstOrDefault(s => s.Season == season && s.Game == game);

            return new PlayerGameViewModel(player, game, season.Value, seasonData);
        }

        [HttpGet("allgames")]
        [AllowAnonymous]
        public async Task<ActionResult<PlayerAllGamesViewModel>> GetPlayerAllGames(string? name, int? id, int? mkcId, string? discordId, string? fc)
        {
            Player? player;
            if (id is not null)
            {
                player = await GetPlayerByIdAsync(id.Value);
            }
            else if (name is not null)
            {
                player = await GetPlayerByNameAsync(name);
            }
            else if (mkcId is not null)
            {
                player = await GetPlayerByRegistryIdAsync(mkcId.Value);
            }
            else if (discordId is not null)
            {
                player = await GetPlayerByDiscordIdAsync(discordId);
            }
            else if (fc is not null)
            {
                player = await GetPlayerByFriendCodeAsync(fc);
            }
            else
            {
                return BadRequest("Must provide name, MKC ID, or discord ID");
            }

            if (player is null)
                return NotFound();

            return new PlayerAllGamesViewModel(player, player.GameRegistrations.Select(r => r.Game.GetStringId()).ToList());
        }

        [HttpGet("details")]
        [AllowAnonymous]
        public ActionResult<PlayerDetailsViewModel> Details(string? name, int? id, string? discordId = null, string? fc = null, GameMode game = GameMode.mk8dx, int? season = null)
        {
            if (!_loungeSettingsService.ValidateGameAndSeason(ref game, ref season, out var error, allowMkWorldFallback: true))
                return BadRequest(error);

            int? playerId;
            if (id is int)
            {
                playerId = id.Value;
            }
            else if (name is not null)
            {
                if (!_playerDetailsCache.TryGetPlayerIdByName(name, out playerId))
                    return NotFound();
            }
            else if (discordId is not null)
            {
                if (!_playerDetailsCache.TryGetPlayerIdByDiscord(discordId, out playerId))
                    return NotFound();
            }
            else if (fc is not null)
            {
                if (!_playerDetailsCache.TryGetPlayerIdByFC(fc, out playerId))
                    return NotFound();
            }
            else
            {
                return NotFound();
            }

            var vm = _playerDetailsViewModelService.GetPlayerDetails(playerId.Value, game, season.Value);
            if (vm is null)
                return NotFound();

            Response.Headers.CacheControl = "public, max-age=180";
            return vm;
        }

        [HttpGet("list")]
        [AllowAnonymous]
        public ActionResult<PlayerListViewModel> Players(int? minMmr, int? maxMmr, GameMode game = GameMode.mk8dx, int? season=null)
        {
            if (!_loungeSettingsService.ValidateGameAndSeason(ref game, ref season, out var error, allowMkWorldFallback: false))
                return BadRequest(error);

            var players = _playerStatCache
                .GetAllStats(game, season.Value)
                .Where(p => (minMmr == null || p.Mmr >= minMmr) && (maxMmr == null || p.Mmr <= maxMmr))
                .Select(p => new PlayerListViewModel.Player(
                    p.Name,
                    p.Id,
                    p.RegistryId ?? -1,
                    p.Mmr,
                    p.DiscordId,
                    p.EventsPlayed))
                .ToList();

            Response.Headers.CacheControl = "public, max-age=600"; // Cache for 10 minutes
            return new PlayerListViewModel { Game = game, Season = season.Value, Players = players };
        }

        [HttpGet("leaderboard")]
        [AllowAnonymous]
        public ActionResult<LeaderboardViewModel> Leaderboard(
            int? season = null,
            GameMode game = GameMode.mk8dx,
            LeaderboardSortOrder sortBy = LeaderboardSortOrder.Mmr,
            int skip = 0,
            int pageSize = 50,
            string? search = null,
            string? country = null,
            int? minMmr = null,
            int? maxMmr = null,
            int? minEventsPlayed = null,
            int? maxEventsPlayed = null,
            DateTime? minCreationDateUtc = null,
            DateTime? maxCreationDateUtc = null)
        {
            if (!_loungeSettingsService.ValidateGameAndSeason(ref game, ref season, out var error, allowMkWorldFallback: true))
                return BadRequest(error);

            if (pageSize < 0)
                return BadRequest("pageSize must be non-negative");

            if (pageSize > 100)
                pageSize = 100;

            if (pageSize == 0)
                pageSize = 50;

            var playerStatsEnumerable = _playerStatCache.GetAllStats(game, season.Value, sortBy).AsEnumerable();
            if (search != null)
            {
                int? playerId = null;
                var registrations = _dbCache.PlayerGameRegistrations[game.GetRegistrationGameMode()];
                if (search.StartsWith("mkc=", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(search[4..], out var mkcId))
                    {
                        playerId = _dbCache.Players.Values.FirstOrDefault(p => p.RegistryId == mkcId && registrations.ContainsKey(p.Id))?.Id;
                    }

                    playerId ??= -1;
                }
                else if (search.StartsWith("discord=", StringComparison.OrdinalIgnoreCase))
                {
                    var discordId = search[8..];
                    playerId = _dbCache.Players.Values.FirstOrDefault(p => discordId.Equals(p.DiscordId, StringComparison.OrdinalIgnoreCase) && registrations.ContainsKey(p.Id))?.Id ?? -1;
                }
                else if (search.StartsWith("switch=", StringComparison.OrdinalIgnoreCase))
                {
                    var switchFc = search[7..];
                    playerId = _dbCache.Players.Values.FirstOrDefault(p => switchFc.Equals(p.SwitchFc, StringComparison.OrdinalIgnoreCase) && registrations.ContainsKey(p.Id))?.Id ?? -1;
                }

                if (playerId == null)
                {
                    var normalized = PlayerUtils.NormalizeName(search);
                    playerStatsEnumerable = playerStatsEnumerable.Where(p => PlayerUtils.NormalizeName(p.Name).Contains(normalized));
                }
                else if (_playerStatCache.TryGetPlayerStatsById(playerId.Value, game, season.Value, out var playerStats))
                {
                    playerStatsEnumerable = [playerStats];
                }
                else
                {
                    playerStatsEnumerable = Enumerable.Empty<PlayerLeaderboardData>();
                }
            }

            if (country != null)
            {
                var normalized = country.ToUpperInvariant();
                playerStatsEnumerable = playerStatsEnumerable.Where(p => p.CountryCode == normalized);
            }

            if (minMmr != null)
                playerStatsEnumerable = playerStatsEnumerable.Where(p => p.Mmr != null && p.Mmr >= minMmr);

            if (maxMmr != null)
                playerStatsEnumerable = playerStatsEnumerable.Where(p => p.Mmr != null && p.Mmr <= maxMmr);

            if (minEventsPlayed != null)
                playerStatsEnumerable = playerStatsEnumerable.Where(p => p.EventsPlayed >= minEventsPlayed);

            if (maxEventsPlayed != null)
                playerStatsEnumerable = playerStatsEnumerable.Where(p => p.EventsPlayed <= maxEventsPlayed);

            if (minCreationDateUtc != null)
                playerStatsEnumerable = playerStatsEnumerable.Where(p => p.AccountCreationDateUtc != null && p.AccountCreationDateUtc >= minCreationDateUtc);

            if (maxCreationDateUtc != null)
                playerStatsEnumerable = playerStatsEnumerable.Where(p => p.AccountCreationDateUtc != null && p.AccountCreationDateUtc <= maxCreationDateUtc);

            int playerCount = 0;
            var data = new List<LeaderboardViewModel.Player>(pageSize);
            foreach (var player in playerStatsEnumerable)
            {
                if (playerCount >= skip && playerCount < skip + pageSize)
                {
                    data.Add(new LeaderboardViewModel.Player
                    {
                        Id = player.Id,
                        OverallRank = !player.HasEvents || player.IsHidden ? null : player.OverallRank,
                        Name = player.Name,
                        Mmr = player.Mmr,
                        MaxMmr = player.MaxMmr,
                        EventsPlayed = player.EventsPlayed,
                        WinRate = player.WinRate,
                        WinsLastTen = player.LastTenWins,
                        LossesLastTen = player.LastTenLosses,
                        GainLossLastTen = player.HasEvents ? player.LastTenGainLoss : null,
                        LastWeekRankChange = player.LastWeekRankChange,
                        LargestGain = player.LargestGain?.Amount,
                        MmrRank = _loungeSettingsService.GetRank(player.Mmr, game, season.Value),
                        MaxMmrRank = _loungeSettingsService.GetRank(player.MaxMmr, game, season.Value),
                        CountryCode = player.CountryCode,
                        AccountCreationDateUtc = player.AccountCreationDateUtc,
                        AverageScore12P = player.AverageScore12P,
                        AverageScore24P = player.AverageScore24P
                    });
                }

                playerCount++;
            }

            Response.Headers.CacheControl = "public, max-age=60";

            return new LeaderboardViewModel
            {
                TotalPlayers = playerCount,
                Data = data,
                Game = game,
                Season = season.Value
            };
        }

        [HttpGet("listPendingNameChanges")]
        public async Task<ActionResult<NameChangeListViewModel>> GetAllPendingNameChanges()
        {
            var nameChanges = await _context.Players
                .Where(p => p.NameChangeRequestedOn != null)
                .Select(p => new NameChangeListViewModel.Player(p.Id, p.DiscordId, p.Name, p.PendingName!, p.NameChangeRequestedOn!.Value, p.NameChangeRequestMessageId))
                .ToListAsync();

            return new NameChangeListViewModel { Players = nameChanges };
        }

        [HttpGet("stats")]
        [AllowAnonymous]
        public ActionResult<StatsViewModel> Stats(GameMode game = GameMode.mk8dx, int? season = null)
        {
            if (!_loungeSettingsService.ValidateGameAndSeason(ref game, ref season, out var error, allowMkWorldFallback: true))
                return BadRequest(error);

            var players = _playerStatCache
                .GetAllStats(game, season.Value)
                .Where(p => p.HasEvents)
                .Select(p => new
                {
                    p.Name,
                    p.Mmr,
                    p.CountryCode
                })
                .ToList();

            var divisionData = new List<StatsViewModel.Division>();
            var countryData = new Dictionary<string, StatsViewModel.Country>(StringComparer.OrdinalIgnoreCase);
            var activityData = new StatsViewModel.Activity
            {
                FormatData = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                DailyActivity = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase),
                DayOfWeekActivity = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Sunday", 0 },
                    { "Monday", 0 },
                    { "Tuesday", 0 },
                    { "Wednesday", 0 },
                    { "Thursday", 0 },
                    { "Friday", 0 },
                    { "Saturday", 0 },
                },
                TierActivity = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            };
            string? currRank = null;
            var mmrTotal = 0;
            var mogiTotal = 0;
            var tierCount = 0;
            var AverageMmr = 0;
            foreach (var player in players)
            {
                mmrTotal += player.Mmr ?? 0;
                string mmrRank = _loungeSettingsService.GetRank(player.Mmr, game, season.Value).Name.ToString();
                if (currRank != mmrRank)
                {
                    if (currRank != null)
                    {
                        divisionData.Add(new StatsViewModel.Division
                        {
                            Tier = currRank,
                            Count = tierCount
                        });
                    }
                    currRank = mmrRank;
                    tierCount = 0;
                }
                tierCount++;

                if (player.CountryCode != null)
                {
                    if (!countryData.ContainsKey(player.CountryCode))
                    {
                        countryData.Add(player.CountryCode, new StatsViewModel.Country
                        {
                            PlayerTotal = 0,
                            TotalAverageMmr = 0,
                            TopSixMmr = 0,
                            TopSixPlayers = new List<StatsViewModel.Player>()
                        });
                    }
                    countryData[player.CountryCode].PlayerTotal += 1;
                    countryData[player.CountryCode].TotalAverageMmr += player.Mmr ?? 0;
                    if (countryData[player.CountryCode].PlayerTotal <= 6)
                    {
                        countryData[player.CountryCode].TopSixMmr += player.Mmr ?? 0;
                        countryData[player.CountryCode].TopSixPlayers.Add(new StatsViewModel.Player
                        {
                            Name = player.Name,
                            Mmr = player.Mmr ?? 0
                        });
                    }
                }
            }

            var median = 0;
            if (players.Count > 0)
            {
                divisionData.Add(new StatsViewModel.Division
                {
                    Tier = currRank!,
                    Count = tierCount
                });

                foreach (var (key, value) in countryData)
                {
                    countryData[key].TotalAverageMmr = value.TotalAverageMmr / value.PlayerTotal;
                    var topDivisor = value.PlayerTotal < 6 ? value.PlayerTotal : 6;
                    countryData[key].TopSixMmr = value.TopSixMmr / topDivisor;
                }

                int mid = players.Count / 2;
                if (players.Count % 2 != 0)
                {
                    median = players[mid].Mmr ?? 0;
                }
                else
                {
                    median = ((players[mid].Mmr ?? 0) + (players[mid - 1].Mmr ?? 0)) / 2;
                }

                AverageMmr = mmrTotal / players.Count;
            }

            var tables = _dbCache.Tables.Values
                .Where(t => t.Game == game && t.Season == season && t.DeletedOn == null && t.VerifiedOn != null)
                .Select(t => new StatsTableViewModel.Table(
                    t.CreatedOn,
                    t.NumTeams,
                    t.Tier,
                    _dbCache.TableScores[t.Id].Count
                ))
                .ToList();

            foreach (var table in tables)
            {
                mogiTotal++;
                if (table.Tier != "SQ")
                {
                    var tableFormat = TableUtils.FormatDisplay(table.NumTeams, table.NumPlayers)!;
                    if (!activityData.FormatData.ContainsKey(tableFormat))
                    {
                        activityData.FormatData[tableFormat] = 0;
                    }
                    activityData.FormatData[tableFormat]++;
                }

                string normalizedTime = table.CreatedOn.ToString("yyyy'-'MM'-'dd");
                activityData.DayOfWeekActivity[table.CreatedOn.DayOfWeek.ToString()]++;

                if (!activityData.DailyActivity.ContainsKey(normalizedTime))
                {
                    var dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Total", 0 }
                    };
                    activityData.DailyActivity.Add(normalizedTime, dictionary);
                }

                var currentTime = activityData.DailyActivity[normalizedTime];
                if (!currentTime.ContainsKey(table.Tier))
                {
                    currentTime.Add(table.Tier, 0);
                }

                currentTime["Total"]++;
                currentTime[table.Tier]++;

                if (!activityData.TierActivity.ContainsKey(table.Tier))
                {
                    activityData.TierActivity.Add(table.Tier, 0);
                }
                activityData.TierActivity[table.Tier]++;
            }

            Response.Headers.CacheControl = "public, max-age=600";

            return new StatsViewModel
            {
                TotalPlayers = players.Count,
                TotalMogis = mogiTotal,
                Game = game,
                Season = season.Value,
                AverageMmr = AverageMmr,
                MedianMmr = median,
                DivisionData = divisionData,
                CountryData = countryData,
                ActivityData = activityData,
                Ranks = _loungeSettingsService.GetRanks(game, season.Value),
                RecordsTierOrder = _loungeSettingsService.GetRecordsTierOrder(game, season.Value),
                DivisionsToTier = _loungeSettingsService.GetDivisionsToTier(game, season.Value)
            };
        }

        [HttpPost("create")]
        public async Task<ActionResult<PlayerViewModel>> Create(string name, int mkcId, int? mmr, GameMode? game = GameMode.mk8dx, string? discordId = null)
        {
            var registryData = await _mkcRegistryApi.GetPlayerRegistryDataAsync(mkcId);
            var normalizedName = PlayerUtils.NormalizeName(name);

            var time = DateTime.UtcNow;
            Player player = new()
            {
                Name = name,
                NormalizedName = normalizedName,
                MKCId = mkcId,
                DiscordId = discordId,
                RegistryId = mkcId,
                CountryCode = registryData.CountryCode,
                SwitchFc = registryData.SwitchFc,
                NameHistory = new List<NameChange> { new NameChange { Name = name, NormalizedName = normalizedName, ChangedOn = DateTime.UtcNow } }
            };

            PlayerSeasonData? seasonData = null;
            if (game is GameMode gameValue)
            {
                player.GameRegistrations = [new() { Game = gameValue.GetRegistrationGameMode(), RegisteredOn = time }];
                if (mmr is int mmrValue)
                {
                    if (!_loungeSettingsService.ValidateCurrentGame(ref gameValue, out var currentSeason, out var error, allowMkWorldFallback: false))
                        return BadRequest(error);

                    seasonData = new() { Mmr = mmrValue, Game = gameValue, Season = currentSeason.Value };
                    player.SeasonData = [seasonData];
                    Placement placement = new() { Mmr = mmrValue, PrevMmr = null, AwardedOn = DateTime.UtcNow, Game = gameValue, Season = currentSeason.Value };
                    player.Placements = [placement];
                }
            }

            _context.Players.Add(player);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                var nameMatchExists = await _context.Players.AnyAsync(p => p.NormalizedName == player.NormalizedName);
                if (nameMatchExists)
                    return BadRequest("User with that name already exists");

                var mkcIdMatchExists = await _context.Players.AnyAsync(p => p.RegistryId == player.RegistryId);
                if (mkcIdMatchExists)
                    return BadRequest("User with that Registry ID already exists");

                var discordIdMatchExists = await _context.Players.AnyAsync(p => p.DiscordId == player.DiscordId);
                if (discordIdMatchExists)
                    return BadRequest("User with that Discord ID already exists");

                throw;
            }

            var vm = seasonData == null ? new PlayerViewModel(player) : new PlayerGameViewModel(player, seasonData.Game, seasonData.Season, seasonData);
            return CreatedAtAction(nameof(GetPlayer), new { name = player.Name }, vm);
        }

        [HttpPost("register")]
        public async Task<ActionResult<PlayerViewModel>> Register(string name, GameMode game = GameMode.mk8dx, int? mmr = null)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            var registrationGameMode = game.GetRegistrationGameMode();

            if (player.GameRegistrations.Any(r => r.Game == registrationGameMode))
                return BadRequest("Player is already registered for this game.");

            var time = DateTime.UtcNow;
            _context.PlayerGameRegistrations.Add(new PlayerGameRegistration
            {
                PlayerId = player.Id,
                Game = registrationGameMode,
                RegisteredOn = time
            });

            PlayerSeasonData? seasonData = null;
            if (mmr is int mmrValue)
            {
                if (!_loungeSettingsService.ValidateCurrentGame(ref game, out var currentSeason, out var error, allowMkWorldFallback: false))
                    return BadRequest(error);

                Placement placement = new() { Mmr = mmrValue, PrevMmr = null, AwardedOn = DateTime.UtcNow, PlayerId = player.Id, Season = currentSeason.Value, Game = game };
                _context.Placements.Add(placement);

                seasonData = new() { Mmr = mmrValue, Game = game, Season = currentSeason.Value, PlayerId = player.Id };
                _context.PlayerSeasonData.Add(seasonData);
            }

            await _context.SaveChangesAsync();

            var vm = seasonData == null ? new PlayerViewModel(player) : new PlayerGameViewModel(player, seasonData.Game, seasonData.Season, seasonData);
            return CreatedAtAction(nameof(Placement), new { name = player.Name }, vm);
        }

        [HttpPost("placement")]
        public async Task<ActionResult<PlayerGameViewModel>> Placement(string name, int mmr, GameMode game = GameMode.mk8dx, bool force=false)
        {
            var player = await GetGamePlayerByNameAsync(name, game);
            if (player is null)
                return NotFound();

            if (!_loungeSettingsService.ValidateCurrentGame(ref game, out var currentSeason, out var error, allowMkWorldFallback: false))
                return BadRequest(error);

            var seasonData = player.SeasonData.FirstOrDefault(s => s.Season == currentSeason.Value && s.Game == game);

            if (seasonData is not null && !force)
            {
                // only look at events that have been verified and aren't deleted
                var eventsPlayed = await _context.TableScores
                    .CountAsync(s => s.PlayerId == player.Id && s.Table.VerifiedOn != null && s.Table.DeletedOn == null && s.Table.Season == currentSeason.Value && s.Table.Game == game);

                if (eventsPlayed > 0)
                    return BadRequest("Player already has been placed and has played a match.");
            }

            Placement placement = new() { Mmr = mmr, PrevMmr = seasonData?.Mmr, AwardedOn = DateTime.UtcNow, PlayerId = player.Id, Season = currentSeason.Value, Game = game };
            _context.Placements.Add(placement);

            if (seasonData is null)
            {
                PlayerSeasonData newSeasonData = new() { Mmr = mmr, Game = game, Season = currentSeason.Value, PlayerId = player.Id };
                _context.PlayerSeasonData.Add(newSeasonData);
                seasonData = newSeasonData;
            }
            else
            {
                seasonData.Mmr = mmr;
            }

            await _context.SaveChangesAsync();

            var vm = new PlayerGameViewModel(player, seasonData.Game, seasonData.Season, seasonData);

            return CreatedAtAction(nameof(Placement), new { name = player.Name }, vm);
        }

        [HttpPost("update/name")]
        public async Task<IActionResult> ChangeName(string name, string newName)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            player.Name = newName;
            player.NormalizedName = PlayerUtils.NormalizeName(newName);
            _context.NameChanges.Add(new NameChange
            {
                Name = newName,
                NormalizedName = player.NormalizedName,
                ChangedOn = DateTime.UtcNow,
                PlayerId = player.Id,
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                var nameMatchExists = await _context.Players.AnyAsync(p => p.NormalizedName == player.NormalizedName);
                if (nameMatchExists)
                    return BadRequest("User with that name already exists");

                throw;
            }

            return NoContent();
        }

        [HttpPost("update/mkcId")]
        public async Task<IActionResult> ChangeMkcId(string name, int newMkcId)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            player.MKCId = newMkcId;
            player.RegistryId = newMkcId;

            var registryData = await _mkcRegistryApi.GetPlayerRegistryDataAsync(newMkcId);
            player.CountryCode = registryData.CountryCode;
            player.SwitchFc = registryData.SwitchFc;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                var mkcIdMatchExists = await _context.Players.AnyAsync(p => p.RegistryId == player.RegistryId);
                if (mkcIdMatchExists)
                    return BadRequest("User with that Registry ID already exists");

                throw;
            }

            return NoContent();
        }

        [HttpPost("update/discordId")]
        public async Task<IActionResult> ChangeDiscordId(string name, string newDiscordId)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            player.DiscordId = newDiscordId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("hide")]
        public async Task<IActionResult> Hide(string name)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            player.IsHidden = true;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("unhide")]
        public async Task<IActionResult> Unhide(string name)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            player.IsHidden = false;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("refreshRegistryData")]
        public async Task<IActionResult> RefreshRegistryData(string name)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            var registryId = player.RegistryId;
            if (registryId != null)
            {
                var registryData = await _mkcRegistryApi.GetPlayerRegistryDataAsync(registryId.Value);
                player.CountryCode = registryData.CountryCode;
                player.SwitchFc = registryData.SwitchFc;
            }
            else
            {
                player.CountryCode = null;
                player.SwitchFc = null;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("requestNameChange")]
        public async Task<ActionResult<NameChangeListViewModel.Player>> RequestNameChange(string name, string newName)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            if (await GetPlayerByNameAsync(newName) is not null)
                return BadRequest("Player with that name is already taken");

            var timeNow = DateTime.UtcNow;
            foreach (var nameChange in player.NameHistory)
            {
                var timeSinceNameChange = timeNow - nameChange.ChangedOn;
                if (timeSinceNameChange < TimeSpan.FromDays(60))
                    return BadRequest($"Player last changed their name less than 60 days ago.");
            }

            player.NameChangeRequestedOn = DateTime.UtcNow;
            player.PendingName = newName;
            await _context.SaveChangesAsync();

            return new NameChangeListViewModel.Player(player.Id, player.DiscordId, player.Name, newName, player.NameChangeRequestedOn.Value, null);
        }

        [HttpPost("setNameChangeMessageId")]
        public async Task<IActionResult> SetNameChangeMessageId(string name, string messageId)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            if (player.NameChangeRequestedOn is null)
                return BadRequest("Player has no pending name change");

            player.NameChangeRequestMessageId = messageId;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("acceptNameChange")]
        public async Task<ActionResult<NameChangeListViewModel.Player>> AcceptNameChange(string name)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            var nameChangeRequestOn = player.NameChangeRequestedOn;
            if (nameChangeRequestOn is null)
                return BadRequest("Player has no pending name change");

            var newName = player.PendingName!;

            if (await GetPlayerByNameAsync(newName) is not null)
                return BadRequest("Player with that name is already taken");

            var timeNow = DateTime.UtcNow;
            foreach (var nameChange in player.NameHistory)
            {
                var timeSinceNameChange = timeNow - nameChange.ChangedOn;
                if (timeSinceNameChange < TimeSpan.FromDays(60))
                    return BadRequest($"Player last changed their name less than 60 days ago.");
            }

            var normalizedNewName = PlayerUtils.NormalizeName(newName);
            var newNameChange = new NameChange
            {
                Name = newName,
                NormalizedName = normalizedNewName,
                ChangedOn = DateTime.UtcNow,
                PlayerId = player.Id,
            };

            var oldName = player.Name;
            var messageId = player.NameChangeRequestMessageId;
            player.Name = newName;
            player.NormalizedName = normalizedNewName;
            player.NameChangeRequestedOn = null;
            player.PendingName = null;
            player.NameChangeRequestMessageId = null;
            _context.NameChanges.Add(newNameChange);
            await _context.SaveChangesAsync();

            return new NameChangeListViewModel.Player(player.Id, player.DiscordId, oldName, newName, nameChangeRequestOn.Value, messageId);
        }

        [HttpPost("rejectNameChange")]
        public async Task<ActionResult<NameChangeListViewModel.Player>> RejectNameChange(string name)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            var nameChangeRequestedOn = player.NameChangeRequestedOn;
            if (nameChangeRequestedOn is null)
                return BadRequest("Player has no pending name change");

            var newName = player.PendingName;
            var messageId = player.NameChangeRequestMessageId;
            player.NameChangeRequestedOn = null;
            player.PendingName = null;
            player.NameChangeRequestMessageId = null;
            await _context.SaveChangesAsync();

            return new NameChangeListViewModel.Player(player.Id, player.DiscordId, player.Name, newName!, nameChangeRequestedOn.Value, messageId);
        }

        // [HttpGet("mkworldSplitMmrSimulation")]
        // [AllowAnonymous]
        public async Task<ActionResult<MkWorldSimulationViewModel>> SimulateMkWorldSplitMmr()
        {
            var tables = _dbCache.Tables.Values.Where(t => t.Game == GameMode.mkworld && t.Season == 1).ToList();
            var bonuses = _dbCache.Bonuses.Values.Where(b => b.Game == GameMode.mkworld && b.Season == 1).ToList();
            var penalties = _dbCache.Penalties.Values.Where(p => p.Game == GameMode.mkworld && p.Season == 1).ToList();
            var placements = _dbCache.Placements.Values.Where(p => p.Game == GameMode.mkworld && p.Season == 1).ToList();

            var events = new List<(DateTime Time, SimulationEventType Type, int EntityId)>();

            tables = tables.Where(t => t.VerifiedOn is not null && t.DeletedOn is null).ToList();
            bonuses = bonuses.Where(b => b.DeletedOn is null).ToList();
            penalties = penalties.Where(p => p.DeletedOn is null).ToList();

            foreach (var table in tables)
            {
                if (table.VerifiedOn != null)
                {
                    events.Add((table.VerifiedOn.Value, SimulationEventType.TableVerify, table.Id));
                    if (table.DeletedOn != null)
                    {
                        events.Add((table.DeletedOn.Value, SimulationEventType.TableRevert, table.Id));
                    }
                }
            }

            foreach (var bonus in bonuses)
            {
                events.Add((bonus.AwardedOn, SimulationEventType.BonusCreate, bonus.Id));
                if (bonus.DeletedOn != null)
                {
                    events.Add((bonus.DeletedOn.Value, SimulationEventType.BonusDelete, bonus.Id));
                }
            }

            foreach (var penalty in penalties)
            {
                events.Add((penalty.AwardedOn, SimulationEventType.PenaltyCreate, penalty.Id));
                if (penalty.DeletedOn != null)
                {
                    events.Add((penalty.DeletedOn.Value, SimulationEventType.PenaltyDelete, penalty.Id));
                }
            }

            var placementsFromPreseason = new Dictionary<int, int>();
            var preseasonData = _dbCache.PlayerSeasonData[(GameMode.mkworld, 0)];

            foreach (var placement in placements)
            {
                events.Add((placement.AwardedOn, SimulationEventType.PlacementCreate, placement.Id));

                if (preseasonData.TryGetValue(placement.PlayerId, out var placementMmr))
                    placementsFromPreseason[placement.PlayerId] = placementMmr.Mmr;
            }

            events.Sort();

            var mmrs12P = new Dictionary<int, int>();
            var mmrs24P = new Dictionary<int, int>();

            var counts12p = new Dictionary<int, int>();
            var counts24p = new Dictionary<int, int>();

            var last12P = new Dictionary<int, DateTime>();
            var last24P = new Dictionary<int, DateTime>();

            var tableMmrChanges = new Dictionary<int, List<(int PlayerId, int PrevMmr, int NewMmr)>>();
            var bonusMmrChanges = new Dictionary<int, (bool Is12P, int PlayerId, int PrevMmr, int NewMmr)>();
            var penaltyMmrChanges = new Dictionary<int, (bool Is12P, int PlayerId, int PrevMmr, int NewMmr)>();

            var tableDatesByPlayer = new Dictionary<int, List<(DateTime, bool Is12P)>>();
            foreach (var table in tables)
            {
                var scores = _dbCache.TableScores[table.Id].Values;
                Debug.Assert(scores.Count is 12 or 24);
                var is12P = scores.Count == 12;
                foreach (var score in scores)
                {
                    if (!tableDatesByPlayer.TryGetValue(score.PlayerId, out var dateList))
                    {
                        dateList = [];
                        tableDatesByPlayer[score.PlayerId] = dateList;
                    }

                    dateList.Add((table.CreatedOn, is12P));

                    if (table.VerifiedOn != null && table.DeletedOn == null)
                    {
                        var countsDict = is12P ? counts12p : counts24p;
                        if (!countsDict.ContainsKey(score.PlayerId))
                            countsDict[score.PlayerId] = 0;
                        countsDict[score.PlayerId]++;
                    }
                }
            }

            // sort all date lists
            foreach (var dateList in tableDatesByPlayer.Values)
            {
                dateList.Sort();
            }

            var placedWithNoEvents = new Dictionary<int, int>();

            foreach (var evt in events)
            {
                switch (evt.Type)
                {
                    case SimulationEventType.TableVerify:
                        {
                            var table = _dbCache.Tables[evt.EntityId];
                            var scores = _dbCache.TableScores[table.Id].Values;
                            var is12P = scores.Count == 12;
                            var mmrDict = is12P ? mmrs12P : mmrs24P;

                            var numTeams = table.NumTeams;

                            var roomMmrs = new Dictionary<int, int>();
                            var missingMmrs = new List<int>();
                            foreach (var score in scores)
                            {
                                if (mmrDict.TryGetValue(score.PlayerId, out var playerMmr))
                                {
                                    roomMmrs[score.PlayerId] = playerMmr;
                                }
                                else
                                {
                                    missingMmrs.Add(score.PlayerId);
                                }
                            }

                            if (missingMmrs.Count > 0)
                            {
                                Debug.Assert(roomMmrs.Count > 4);
                                var roomAverage = roomMmrs.Values.Average();
                                foreach (var playerId in missingMmrs)
                                {
                                    if (placementsFromPreseason.TryGetValue(playerId, out var placementMmr))
                                        roomMmrs[playerId] = placementMmr;
                                    else
                                        roomMmrs[playerId] = (int)Math.Round(roomAverage);
                                }
                            }

                            var scoresGrouped = new (string Player, int Score, int CurrentMmr, double Multiplier)[numTeams][];
                            for (int i = 0; i < numTeams; i++)
                            {
                                scoresGrouped[i] = scores
                                    .Where(score => score.Team == i)
                                    .Select(s => (s.PlayerId.ToString(), s.Score, roomMmrs[s.PlayerId], s.Multiplier))
                                    .ToArray();
                            }

                            var mmrDeltas = TableUtils.GetMMRDeltas(scoresGrouped);
                            var mmrChanges = new List<(int PlayerId, int PrevMmr, int NewMmr)>();
                            foreach (var score in scores)
                            {
                                var playerId = score.PlayerId;
                                var delta = mmrDeltas[playerId.ToString()];
                                var prevMmr = roomMmrs[playerId];
                                var newMmr = Math.Max(prevMmr + delta, 0);

                                mmrDict[playerId] = newMmr;
                                mmrChanges.Add((playerId, prevMmr, newMmr));

                                if (table.DeletedOn is null)
                                {
                                    if (is12P)
                                        last12P[playerId] = table.CreatedOn;
                                    else
                                        last24P[playerId] = table.CreatedOn;
                                }
                            }

                            tableMmrChanges[evt.EntityId] = mmrChanges;
                            break;
                        }
                    case SimulationEventType.TableRevert:
                        {
                            Debug.Assert(false);
                            var changes = tableMmrChanges[evt.EntityId];
                            var mmrDict = changes.Count == 12 ? mmrs12P : mmrs24P;
                            foreach (var change in changes)
                            {
                                var diff = change.NewMmr - change.PrevMmr;
                                var newMmr = Math.Max(mmrDict[change.PlayerId] - diff, 0);
                                mmrDict[change.PlayerId] = newMmr;
                            }
                            break;
                        }
                    case SimulationEventType.BonusCreate:
                        {
                            var bonus = _dbCache.Bonuses[evt.EntityId];
                            var is12P = NearestIs12P(bonus.PlayerId, evt.Time, beforeOnly: true);
                            Debug.Assert(is12P is not null);

                            var mmrDict = is12P.Value ? mmrs12P : mmrs24P;
                            var prevMmr = mmrDict[bonus.PlayerId];
                            var amount = bonus.NewMmr - bonus.PrevMmr;
                            var newMmr = prevMmr + amount;
                            Debug.Assert(newMmr >= 0);

                            mmrDict[bonus.PlayerId] = newMmr;
                            bonusMmrChanges[bonus.Id] = (is12P.Value, bonus.PlayerId, prevMmr, newMmr);
                            break;
                        }
                    case SimulationEventType.BonusDelete:
                        {
                            Debug.Assert(false);
                            var change = bonusMmrChanges[evt.EntityId];
                            var mmrDict = change.Is12P ? mmrs12P : mmrs24P;
                            var diff = change.NewMmr - change.PrevMmr;
                            var newMmr = Math.Max(mmrDict[change.PlayerId] - diff, 0);
                            mmrDict[change.PlayerId] = newMmr;
                            break;
                        }
                    case SimulationEventType.PenaltyCreate:
                        {
                            var penalty = _dbCache.Penalties[evt.EntityId];
                            var is12P = NearestIs12P(penalty.PlayerId, evt.Time, beforeOnly: true);
                            if (is12P is null)
                            {
                                penaltyMmrChanges[penalty.Id] = (false, penalty.PlayerId, penalty.NewMmr, penalty.NewMmr);
                                break;
                            }

                            var mmrDict = is12P.Value ? mmrs12P : mmrs24P;
                            if (!mmrDict.TryGetValue(penalty.PlayerId, out var prevMmr))
                            {
                                prevMmr = penalty.PrevMmr;
                                mmrDict[penalty.PlayerId] = prevMmr;
                            }

                            var amount = penalty.NewMmr - penalty.PrevMmr;
                            var newMmr = Math.Max(prevMmr - amount, 0);

                            mmrDict[penalty.PlayerId] = newMmr;
                            penaltyMmrChanges[penalty.Id] = (is12P.Value, penalty.PlayerId, prevMmr, newMmr);
                            break;
                        }
                    case SimulationEventType.PenaltyDelete:
                        {
                            Debug.Assert(false);
                            var change = penaltyMmrChanges[evt.EntityId];
                            var mmrDict = change.Is12P ? mmrs12P : mmrs24P;
                            var diff = change.NewMmr - change.PrevMmr;
                            if (diff == 0)
                                break;

                            var newMmr = change.PrevMmr - diff;
                            Debug.Assert(newMmr >= 0);
                            mmrDict[change.PlayerId] = newMmr;
                            break;
                        }
                    case SimulationEventType.PlacementCreate:
                        {
                            var placement = _dbCache.Placements[evt.EntityId];
                            if (placement.PrevMmr is null)
                            {
                                var is12P = NearestIs12P(placement.PlayerId, evt.Time, beforeOnly: false);
                                if (is12P is null)
                                {
                                    placedWithNoEvents[placement.PlayerId] = placement.Mmr;
                                    break;
                                }

                                var mmrDict = is12P.Value ? mmrs12P : mmrs24P;
                                Debug.Assert(!mmrDict.ContainsKey(placement.PlayerId));
                                mmrDict[placement.PlayerId] = placement.Mmr;
                            }
                            else
                            {
                                if (mmrs12P.ContainsKey(placement.PlayerId))
                                    mmrs12P[placement.PlayerId] = placement.Mmr;
                                if (mmrs24P.ContainsKey(placement.PlayerId))
                                    mmrs24P[placement.PlayerId] = placement.Mmr;
                            }

                            break;
                        }
                }
            }

            var playerNames = _dbCache.Players.ToDictionary(p => p.Key, p => p.Value.Name);

            return new MkWorldSimulationViewModel
            {
                MmrsWithNoTables = placedWithNoEvents.Select(kvp =>
                {
                    var pId = kvp.Key;
                    var name = playerNames[pId];
                    var mmr = kvp.Value;
                    return new MkWorldSimulationViewModel.PlayerWithNoTableData(pId, name, mmr);
                }).ToList(),
                Mmrs12P = mmrs12P.Select(kvp => 
                {
                    var pId = kvp.Key;
                    var name = playerNames[pId];
                    var eventsPlayed = counts12p[pId];
                    return new MkWorldSimulationViewModel.PlayerData(pId, name, kvp.Value, eventsPlayed, last12P[pId]);
                }).ToList(),
                Mmrs24P = mmrs24P.Select(kvp =>
                {
                    var pId = kvp.Key;
                    var name = playerNames[pId];
                    var eventsPlayed = counts24p[pId];
                    return new MkWorldSimulationViewModel.PlayerData(pId, name, kvp.Value, eventsPlayed, last24P[pId]);
                }).ToList(),
            };

            bool? NearestIs12P(int playerId, DateTime time, bool beforeOnly)
            {
                var dateList = tableDatesByPlayer.GetValueOrDefault(playerId, []);
                if (dateList.Count == 0)
                    return null;

                (DateTime, bool) best = dateList[0];
                if (beforeOnly && best.Item1 > time)
                    return null;

                var bestDiff = (best.Item1 - time).Duration();

                for (int i = 1; i < dateList.Count; i++)
                {
                    var date = dateList[i];
                    if (beforeOnly && date.Item1 > time)
                        break;

                    var diff = (date.Item1 - time).Duration();
                    if (diff < bestDiff)
                    {
                        best = date;
                        bestDiff = diff;
                    }
                }

                return best.Item2;
            }
        }

        public class MkWorldSimulationViewModel
        {
            public List<PlayerWithNoTableData> MmrsWithNoTables { get; set; }
            public List<PlayerData> Mmrs12P { get; set; }
            public List<PlayerData> Mmrs24P { get; set; }

            public record PlayerWithNoTableData(int PlayerId, string PlayerName, int Mmr);
            public record PlayerData(int PlayerId, string PlayerName, int Mmr, int EventsPlayed, DateTime LastPlayed);
        }

        public enum SimulationEventType
        {
            TableVerify,
            TableRevert,
            BonusCreate,
            BonusDelete,
            PenaltyCreate,
            PenaltyDelete,
            PlacementCreate,
        }

        private Task<Player?> GetPlayerByIdAsync(int id) =>
            _context.Players
                .Include(p => p.GameRegistrations)
                .SingleOrDefaultAsync(p => p.Id == id);

        private Task<Player?> GetPlayerByNameAsync(string name) =>
            _context.Players
                .Include(p => p.GameRegistrations)
                .Include(p => p.NameHistory)
                .SingleOrDefaultAsync(p => p.NormalizedName == PlayerUtils.NormalizeName(name));

        private Task<Player?> GetPlayerByRegistryIdAsync(int registryId) =>
            _context.Players
                .Include(p => p.GameRegistrations)
                .SingleOrDefaultAsync(p => p.RegistryId == registryId);

        private Task<Player?> GetPlayerByDiscordIdAsync(string discordId) =>
            _context.Players
                .Include(p => p.GameRegistrations)
                .SingleOrDefaultAsync(p => p.DiscordId == discordId);

        private Task<Player?> GetPlayerByFriendCodeAsync(string fc) =>
            _context.Players
                .Include(p => p.GameRegistrations)
                .SingleOrDefaultAsync(p => p.SwitchFc == fc);

        private Task<Player?> GetGamePlayerByIdAsync(int id, GameMode game) =>
            _context.PlayerGameRegistrations
                .Include(g => g.Player.SeasonData)
                .Where(p => p.Game == game.GetRegistrationGameMode())
                .Select(p => p.Player)
                .SingleOrDefaultAsync(p => p.Id == id);

        private Task<Player?> GetGamePlayerByNameAsync(string name, GameMode game) =>
            _context.PlayerGameRegistrations
                .Include(g => g.Player.SeasonData)
                .Where(p => p.Game == game.GetRegistrationGameMode())
                .Select(p => p.Player)
                .SingleOrDefaultAsync(p => p.NormalizedName == PlayerUtils.NormalizeName(name));

        private Task<Player?> GetGamePlayerByRegistryIdAsync(int registryId, GameMode game) =>
            _context.PlayerGameRegistrations
                .Include(g => g.Player.SeasonData)
                .Where(p => p.Game == game.GetRegistrationGameMode())
                .Select(p => p.Player)
                .SingleOrDefaultAsync(p => p.RegistryId == registryId);

        private Task<Player?> GetGamePlayerByDiscordIdAsync(string discordId, GameMode game) =>
            _context.PlayerGameRegistrations
                .Include(g => g.Player.SeasonData)
                .Where(p => p.Game == game.GetRegistrationGameMode())
                .Select(p => p.Player)
                .SingleOrDefaultAsync(p => p.DiscordId == discordId);

        private Task<Player?> GetGamePlayerByFriendCodeAsync(string fc, GameMode game) =>
            _context.PlayerGameRegistrations
                .Include(g => g.Player.SeasonData)
                .Where(p => p.Game == game.GetRegistrationGameMode())
                .Select(p => p.Player)
                .SingleOrDefaultAsync(p => p.SwitchFc == fc);
    }
}
