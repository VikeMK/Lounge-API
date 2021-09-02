namespace Lounge.Web.Data.Entities.ChangeTracking
{
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
}
