﻿using Lounge.Web.Models.Enums;
using Lounge.Web.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Models.ViewModels
{
    public class TableDetailsViewModel
    {
        public TableDetailsViewModel(int id, Game game, int season, DateTime createdOn, DateTime? verifiedOn, DateTime? deletedOn, int numTeams, int numPlayers, string url, string tier, List<Team> teams, string? tableMessageId, string? updateMessageId, string? authorId)
        {
            Id = id;
            Game = game;
            Season = season;
            CreatedOn = createdOn;
            VerifiedOn = verifiedOn;
            DeletedOn = deletedOn;
            NumTeams = numTeams;
            NumPlayers = numPlayers;
            Url = url ?? throw new ArgumentNullException(nameof(url));
            Tier = tier?.ToUpperInvariant() ?? throw new ArgumentNullException(nameof(tier));
            Teams = teams ?? throw new ArgumentNullException(nameof(teams));
            TableMessageId = tableMessageId;
            UpdateMessageId = updateMessageId;
            AuthorId = authorId;
        }

        [Display(Name = "Table ID")]
        public int Id { get; set; }

        [Display(Name = "Game")]
        public Game Game { get; set; }

        [Display(Name = "Season")]
        public int Season { get; set; }

        [Display(Name = "Time Created")]
        public DateTime CreatedOn { get; set; }

        [Display(Name = "Time Verified")]
        [DisplayFormat(NullDisplayText = "Not Verified Yet")]
        public DateTime? VerifiedOn { get; set; }

        [Display(Name = "Time Deleted")]
        public DateTime? DeletedOn { get; set; }

        public int NumTeams { get; set; }

        public int NumPlayers { get; set; }

        public string Format => TableUtils.FormatDisplay(NumTeams, NumPlayers) ?? $"Invalid Format";

        [Display(Name = "Table Image")]
        public string Url { get; set; }

        public string Tier { get; set; }

        public List<Team> Teams { get; set; }

        public string? TableMessageId { get; set; }

        public string? UpdateMessageId { get; set; }

        public string? AuthorId { get; set; }

        public class Team
        {
            public Team(int rank, List<TableScore> scores)
            {
                Scores = scores ?? throw new ArgumentNullException(nameof(scores));
                Rank = rank;
            }

            public int Rank { get; set; }

            public List<TableScore> Scores { get; set; }
        }

        public class TableScore
        {
            public TableScore(int score, double multiplier, int? prevMmr, int? newMmr, int playerId, string playerName, string? playerDiscordId, string? playerCountryCode)
            {
                Score = score;
                Multiplier = multiplier;
                PrevMmr = prevMmr;
                NewMmr = newMmr;
                PlayerId = playerId;
                PlayerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
                PlayerDiscordId = playerDiscordId;
                PlayerCountryCode = playerCountryCode;
            }

            public int Score { get; set; }
            public double Multiplier { get; set; }

            [Display(Name = "Multiplier")]
            public string? MultiplierString => Multiplier == 1 ? null : $"{Multiplier:F2}x";

            [Display(Name = "Previous MMR")]
            public int? PrevMmr { get; set; }

            [Display(Name = "New MMR")]
            public int? NewMmr { get; set; }

            [Display(Name = "Change")]
            [DisplayFormat(DataFormatString = "{0:+#;-#;0}")]
            public int? Delta => NewMmr - PrevMmr;

            public int PlayerId { get; set; }

            [Display(Name = "Player")]
            public string PlayerName { get; set; }

            public string? PlayerDiscordId { get; set; }

            public string? PlayerCountryCode { get; set; }

            public bool? IsNewPeakMmr { get; set; }
        }
    }
}

