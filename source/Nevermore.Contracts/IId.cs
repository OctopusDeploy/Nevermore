namespace Nevermore.Contracts
{
    /// <typeparam name="TId">The type of the ID, for example, string.</typeparam>
    public interface IId<out TId>
    {
        TId Id { get; }
    }

    public interface IId : IId<string>
    {
    }
}
