namespace Lounge.Web.Models
{
    public class ChangeTrackingCurrentVersion
    {
        public long CurrentVersion { get; set; }
    }

    public abstract class Change
    {
        public long Version { get; set; }
        public long? CreationVersion { get; set; }
        public char Operation { get; set; }
        public byte[]? Columns { get; set; }
        public byte[]? Context { get; set; }
    }

    public abstract class Change<TEntity> : Change
        where TEntity : class
    {
        public TEntity? Entity { get; set; }
    }

    public class BonusChange : Change<Bonus>
    {
        public int Id { get; set; }
    }

    public class PenaltyChange : Change<Penalty>
    {
        public int Id { get; set; }
    }

    public class PlacementChange : Change<Placement>
    {
        public int Id { get; set; }
    }

    public class PlayerChange : Change<Player>
    {
        public int Id { get; set; }
    }

    public class PlayerSeasonDataChange : Change<PlayerSeasonData>
    {
        public int Season { get; set; }
        public int PlayerId { get; set; }
    }

    public class TableChange : Change<Table>
    {
        public int Id { get; set; }
    }

    public class TableScoreChange : Change<TableScore>
    {
        public int TableId { get; set; }
        public int PlayerId { get; set; }
    }
}
