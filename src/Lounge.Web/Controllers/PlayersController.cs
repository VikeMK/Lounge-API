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
using Lounge.Web.Controllers.ValidationAttributes;
using Lounge.Web.Data.Entities;
using Lounge.Web.Data.ChangeTracking;
using Lounge.Web.Models.Enums;

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
        public async Task<ActionResult<PlayerViewModel>> GetPlayer(string? name, int? id, int? mkcId, string? discordId, string? fc, Game game = Game.MK8DX, [ValidSeason]int? season = null)
        {
            season ??= _loungeSettingsService.CurrentSeason[game];

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

            var seasonData = player.SeasonData.FirstOrDefault(s => s.Season == season && s.Game == (int)game);

            return PlayerUtils.GetPlayerViewModel(player, seasonData);
        }

        [HttpGet("allgames")]
        [AllowAnonymous]
        public async Task<ActionResult<PlayerViewModel>> GetPlayerAllGames(string? name, int? id, int? mkcId, string? discordId, string? fc)
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

            return new PlayerViewModel
            {
                Id = player.Id,
                DiscordId = player.DiscordId,
                MKCId = player.RegistryId ?? -1,
                RegistryId = player.RegistryId,
                Name = player.Name,
                CountryCode = player.CountryCode,
                SwitchFc = player.SwitchFc,
                IsHidden = player.IsHidden,
                Registrations = player.GameRegistrations.Select(r => ((Game)r.Game).GetStringId()).ToList()
            };
        }

        [HttpGet("details")]
        [AllowAnonymous]
        public ActionResult<PlayerDetailsViewModel> Details(string? name, int? id, string? discordId = null, string? fc = null, Game game = Game.MK8DX, [ValidSeason] int? season = null)
        {
            season ??= _loungeSettingsService.CurrentSeason[game];

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

            return vm;
        }

        [HttpGet("list")]
        [AllowAnonymous]
        public ActionResult<PlayerListViewModel> Players(int? minMmr, int? maxMmr, Game game = Game.MK8DX, [ValidSeason] int? season=null)
        {
            season ??= _loungeSettingsService.CurrentSeason[game];

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

            return new PlayerListViewModel { Players = players };
        }

        [HttpGet("leaderboard")]
        [AllowAnonymous]
        public ActionResult<LeaderboardViewModel> Leaderboard(
            int season,
            Game game = Game.MK8DX,
            LeaderboardSortOrder sortBy = LeaderboardSortOrder.Mmr,
            int skip = 0,
            int pageSize = 50,
            string? search = null,
            string? country = null,
            int? minMmr = null,
            int? maxMmr = null,
            int? minEventsPlayed = null,
            int? maxEventsPlayed = null)
        {
            if (pageSize < 0)
                return BadRequest("pageSize must be non-negative");

            if (pageSize > 100)
                pageSize = 100;

            if (pageSize == 0)
                pageSize = 50;

            var playerStatsEnumerable = _playerStatCache.GetAllStats(game, season, sortBy).AsEnumerable();
            if (search != null)
            {
                int? playerId = null;
                var registrations = _dbCache.PlayerGameRegistrations[game];
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
                else if (_playerStatCache.TryGetPlayerStatsById(playerId.Value, game, season, out var playerStats))
                {
                    playerStatsEnumerable = new[] { playerStats };
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
                        LargestGain = player.LargestGain?.Amount,
                        LargestLoss = player.LargestLoss?.Amount,
                        NoSQAverageScore = player.NoSQAverageScore,
                        NoSQAverageScoreLastTen = player.NoSQAverageLastTen,
                        MmrRank = _loungeSettingsService.GetRank(player.Mmr, game, season),
                        MaxMmrRank = _loungeSettingsService.GetRank(player.MaxMmr, game, season),
                        CountryCode = player.CountryCode
                    });
                }

                playerCount++;
            }

            return new LeaderboardViewModel
            {
                TotalPlayers = playerCount,
                Data = data
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
        public ActionResult<StatsViewModel> Leaderboard(Game game = Game.MK8DX, [ValidSeason] int? season = null)
        {
            season ??= _loungeSettingsService.CurrentSeason[game];

            var players = _playerStatCache
                .GetAllStats(game, season.Value)
                .Where(p => p.HasEvents)
                .Select(p => new StatsPlayerViewModel.Player(
                    p.Name,
                    p.Mmr,
                    p.CountryCode
                    ))
                .ToList();

            var divisionData = new List<StatsViewModel.Division>();
            var countryData = new Dictionary<string, StatsViewModel.Country>();
            var activityData = new StatsViewModel.Activity
            {
                FormatData = new Dictionary<string, int>(),
                DailyActivity = new Dictionary<string, Dictionary<string, int>>(),
                DayOfWeekActivity = new Dictionary<string, int>
                {
                    { "Sunday", 0 },
                    { "Monday", 0 },
                    { "Tuesday", 0 },
                    { "Wednesday", 0 },
                    { "Thursday", 0 },
                    { "Friday", 0 },
                    { "Saturday", 0 },
                },
                TierActivity = new Dictionary<string, int>()
            };
            string? currRank = null;
            var mmrTotal = 0;
            var mogiTotal = 0;
            var tierCount = 0;
            var AverageMmr = 0;
            foreach (var player in players)
            {
                mmrTotal += player.Mmr ?? 0;
                string mmrRank = _loungeSettingsService.GetRank(player.Mmr, game, season.Value)!.Name.ToString();
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
                    Tier = currRank,
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
                    if (players[mid].Mmr != null)
                    {
                        median = players[mid].Mmr.Value;
                    }
                }
                else
                {
                    if (players[mid].Mmr != null)
                    {
                        median = (players[mid].Mmr.Value + players[mid - 1].Mmr.Value) / 2;
                    }
                }

                AverageMmr = mmrTotal / players.Count;
            }

            var tables = _dbCache.Tables.Values
                .Where(t => t.Game == (int)game && t.Season == season && t.DeletedOn == null && t.VerifiedOn != null)
                .Select(t => new StatsTableViewModel.Table(
                    t.CreatedOn,
                    t.NumTeams,
                    t.Tier
                ))
                .ToList();

            foreach (var table in tables)
            {
                mogiTotal++;
                if (table.Tier != "SQ")
                {
                    var tableFormat = TableUtils.FormatDisplay(table.NumTeams)!;
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
                    var dictionary = new Dictionary<string, int>
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

            return new StatsViewModel
            {
                TotalPlayers = players.Count,
                TotalMogis = mogiTotal,
                AverageMmr = AverageMmr,
                MedianMmr = median,
                DivisionData = divisionData,
                CountryData = countryData,
                ActivityData = activityData
            };
        }

        [HttpPost("create")]
        public async Task<ActionResult<PlayerViewModel>> Create(string name, int mkcId, int? mmr, Game? game = Game.MK8DX, string? discordId = null)
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


            if (game is Game gameValue)
            {
                var season = _loungeSettingsService.CurrentSeason[gameValue];

                player.GameRegistrations = [new() { Game = (int)gameValue, RegisteredOn = time }];
                if (mmr is int mmrValue)
                {
                    PlayerSeasonData? seasonData = new() { Mmr = mmrValue, Game = (int)game, Season = season };
                    player.SeasonData = new List<PlayerSeasonData> { seasonData };
                    Placement placement = new() { Mmr = mmrValue, PrevMmr = null, AwardedOn = DateTime.UtcNow, Game = (int)game, Season = season };
                    player.Placements = new List<Placement> { placement };
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

            var vm = new PlayerViewModel
            {
                Id = player.Id,
                DiscordId = player.DiscordId,
                MKCId = player.RegistryId ?? -1,
                RegistryId = player.RegistryId,
                Name = player.Name,
                CountryCode = player.CountryCode,
                SwitchFc = player.SwitchFc,
                IsHidden = player.IsHidden,
                Mmr = mmr,
                MaxMmr = null,
            };

            return CreatedAtAction(nameof(GetPlayer), new { name = player.Name }, vm);
        }

        [HttpPost("register")]
        public async Task<ActionResult<PlayerViewModel>> Register(string name, Game game = Game.MK8DX, int? mmr = null)
        {
            var player = await GetPlayerByNameAsync(name);
            if (player is null)
                return NotFound();

            if (player.GameRegistrations.Any(r => r.Game == (int)game))
                return BadRequest("Player is already registered for this game.");

            var time = DateTime.UtcNow;
            player.GameRegistrations.Add(new PlayerGameRegistration
            {
                Game = (int)game,
                RegisteredOn = time
            });

            PlayerSeasonData? seasonData = null;
            if (mmr is int mmrValue)
            {
                var season = _loungeSettingsService.CurrentSeason[game];

                Placement placement = new() { Mmr = mmrValue, PrevMmr = null, AwardedOn = DateTime.UtcNow, PlayerId = player.Id, Season = season, Game = (int)game };
                _context.Placements.Add(placement);

                seasonData = new() { Mmr = mmrValue, Game = (int)game, Season = season, PlayerId = player.Id };
                _context.PlayerSeasonData.Add(seasonData);
            }

            await _context.SaveChangesAsync();

            var vm = PlayerUtils.GetPlayerViewModel(player, seasonData);

            return CreatedAtAction(nameof(Placement), new { name = player.Name }, vm);
        }

        [HttpPost("placement")]
        public async Task<ActionResult<PlayerViewModel>> Placement(string name, int mmr, Game game = Game.MK8DX, bool force=false)
        {
            var player = await GetGamePlayerByNameAsync(name, game);
            if (player is null)
                return NotFound();

            var season = _loungeSettingsService.CurrentSeason[game];
            var seasonData = player.SeasonData.FirstOrDefault(s => s.Season == season && s.Game == (int)game);

            if (seasonData is not null && !force)
            {
                // only look at events that have been verified and aren't deleted
                var eventsPlayed = await _context.TableScores
                    .CountAsync(s => s.PlayerId == player.Id && s.Table.VerifiedOn != null && s.Table.DeletedOn == null && s.Table.Season == season && s.Table.Game == (int)game);

                if (eventsPlayed > 0)
                    return BadRequest("Player already has been placed and has played a match.");
            }

            Placement placement = new() { Mmr = mmr, PrevMmr = seasonData?.Mmr, AwardedOn = DateTime.UtcNow, PlayerId = player.Id, Season = season, Game = (int)game };
            _context.Placements.Add(placement);

            if (seasonData is null)
            {
                PlayerSeasonData newSeasonData = new() { Mmr = mmr, Game = (int)game, Season = season, PlayerId = player.Id };
                _context.PlayerSeasonData.Add(newSeasonData);
            }
            else
            {
                seasonData.Mmr = mmr;
            }

            await _context.SaveChangesAsync();

            var vm = PlayerUtils.GetPlayerViewModel(player, seasonData);

            return CreatedAtAction(nameof(Placement), new { name = player.Name }, vm);
        }

        // TODO: Get this working
        //[HttpPost("bulkPlacement")]
        //public async Task<IActionResult> BulkPlacement([FromBody] BulkPlacementViewModel request)
        //{
        //    var game = request.Game;
        //    var season = _loungeSettingsService.CurrentSeason[game];

        //    // If any matches have been played at all for this season yet, return an error
        //    if (await _context.TableScores.AnyAsync(s => s.Table.Season == season && s.Table.Game == (int)game))
        //        return BadRequest("Cannot bulk place players if matches have already been played for the season.");

        //    var alreadyPlacedIds = (await _context.Placements.Where(p => p.Season == season && p.Game == (int)game)
        //        .Select(p => p.PlayerId)
        //        .ToListAsync()).ToHashSet();

        //    var time = DateTime.UtcNow;

        //    foreach (var playerPlacement in request.PlayerPlacements)
        //    {
        //        // Get player id using cache
        //        if (!_playerDetailsCache.TryGetPlayerIdByName(playerPlacement.Name, out int? playerId))
        //            return BadRequest($"Player {playerPlacement.Name} not found.");

        //        if (alreadyPlacedIds.Contains(playerId.Value))
        //            return BadRequest($"Player {playerPlacement.Name} ({playerId.Value}) has already been placed.");

        //        Placement placement = new() { Mmr = playerPlacement.Mmr, PrevMmr = null, AwardedOn = time, PlayerId = playerId.Value, Season = season, Game = (int)game };
        //        PlayerSeasonData newSeasonData = new() { Mmr = playerPlacement.Mmr, Game = (int)game, Season = season, PlayerId = playerId.Value };

        //        _context.Placements.Add(placement);
        //        _context.PlayerSeasonData.Add(newSeasonData);
        //    }

        //    await _context.SaveChangesAsync();

        //    return NoContent();
        //}

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

        private Task<Player?> GetGamePlayerByIdAsync(int id, Game game) =>
            _context.PlayerGameRegistrations
                .Include(g => g.Player.SeasonData)
                .Where(p => p.Game == (int)game)
                .Select(p => p.Player)
                .SingleOrDefaultAsync(p => p.Id == id);

        private Task<Player?> GetGamePlayerByNameAsync(string name, Game game) =>
            _context.PlayerGameRegistrations
                .Include(g => g.Player.SeasonData)
                .Where(p => p.Game == (int)game)
                .Select(p => p.Player)
                .SingleOrDefaultAsync(p => p.NormalizedName == PlayerUtils.NormalizeName(name));

        private Task<Player?> GetGamePlayerByRegistryIdAsync(int registryId, Game game) =>
            _context.PlayerGameRegistrations
                .Include(g => g.Player.SeasonData)
                .Where(p => p.Game == (int)game)
                .Select(p => p.Player)
                .SingleOrDefaultAsync(p => p.RegistryId == registryId);

        private Task<Player?> GetGamePlayerByDiscordIdAsync(string discordId, Game game) =>
            _context.PlayerGameRegistrations
                .Include(g => g.Player.SeasonData)
                .Where(p => p.Game == (int)game)
                .Select(p => p.Player)
                .SingleOrDefaultAsync(p => p.DiscordId == discordId);

        private Task<Player?> GetGamePlayerByFriendCodeAsync(string fc, Game game) =>
            _context.PlayerGameRegistrations
                .Include(g => g.Player.SeasonData)
                .Where(p => p.Game == (int)game)
                .Select(p => p.Player)
                .SingleOrDefaultAsync(p => p.SwitchFc == fc);

        private PlayerLeaderboardData? GetPlayerStats(int id, Game game, int season)
        {
            PlayerLeaderboardData? playerStat = null;
            if (id != -1)
            {
                _playerStatCache.TryGetPlayerStatsById(id, game, season, out playerStat);
            }

            return playerStat;
        }
    }
}
