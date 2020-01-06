namespace Nevermore.Contracts
{
    public interface IId
    {
        string Id { get; }
    }

    public interface IId<out TId> where TId : IIdWrapper
    {
        TId Id { get; }
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