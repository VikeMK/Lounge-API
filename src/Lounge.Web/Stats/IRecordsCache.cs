using Lounge.Web.Models.Enums;

namespace Lounge.Web.Stats
{
    public interface IRecordsCache
    {
        RecordsCache.SeasonRecords GetRecords(Game game, int season);
    }
}