namespace Nevermore.Contracts
{
    /// <typeparam name="T">Please use something that serializes to/from a string</typeparam>
    public interface IId<out T>
    {
        T Id { get; }
    }

    public interface IId : IId<string>
    {
    }
}
