namespace Lounge.Web.Data.Entities.ChangeTracking
{
    public class TableScoreChange : Change<TableScore>
    {
        public int TableId { get; set; }
        public int PlayerId { get; set; }
    }
}
