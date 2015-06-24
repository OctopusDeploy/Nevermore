namespace Nevermore.IntegrationTests
{
    public interface IRelationalStoreFactory
    {
        RelationalStore RelationalStore { get; }
    }
}