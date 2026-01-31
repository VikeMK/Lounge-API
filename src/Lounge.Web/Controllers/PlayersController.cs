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
