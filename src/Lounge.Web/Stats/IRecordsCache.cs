namespace Lounge.Web.Stats
{
    public interface IRecordsCache
    {
        RecordsCache.SeasonRecords GetRecords(int season);
    }
}