using System;
using Lounge.Web.Models.Enums;

namespace Lounge.Web.Models.ViewModels
{
    public class PenaltyRequestViewModel
    {
        public int Id { get; init; }
        public Game Game { get; init; }
        public string PenaltyName { get; init; } = default!;
        public int TableId { get; init; }
        public int NumberOfRaces { get; init; }
        public int PlayerId { get; init; }
        public required string PlayerName { get; init; }
        public int ReporterId { get; init; }
        public required string ReporterName { get; init; }
    }
}
