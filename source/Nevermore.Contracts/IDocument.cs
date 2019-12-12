namespace Nevermore.Contracts
{
    public interface IDocument<out TId> : IId<TId>, INamed
    {
    }

    public interface IDocument : IDocument<string>, IId
    {
    }
}