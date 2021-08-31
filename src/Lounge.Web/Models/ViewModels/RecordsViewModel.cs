using Lounge.Web.Stats;

namespace Lounge.Web.Models.ViewModels
{
    public record RecordsViewModel(int Season, RecordsCache.SeasonRecords Records);
}
