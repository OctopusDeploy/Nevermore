namespace Nevermore
{
    public interface IDatabaseMigrator
    {
        void Migrate(IRelationalStore store);
    }
}