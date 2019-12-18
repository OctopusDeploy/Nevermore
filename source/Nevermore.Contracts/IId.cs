namespace Nevermore.Contracts
{
    /// <typeparam name="TId">The type of the ID, for example, string.</typeparam>
    public interface IId<out TId>
        where TId : IIdWrapper
    {
        TId Id { get; }
    }

    public interface IId : IId<LegacyStringId>
    {
    }

    public interface IIdWrapper
    {
        string Value { get; }
    }

    public interface IIdWrapperCoupledToDocument<T> : IIdWrapper
    {
        
    }

    public class LegacyStringId : IIdWrapper
    {
        public string Value { get; }
    }
}
