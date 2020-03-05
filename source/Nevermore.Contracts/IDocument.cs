namespace Nevermore.Contracts
{
    public interface IDocument : IId, INamed
    {
    }

    public interface IDocument<out TId> : IId<TId>, INamed where TId : IIdWrapper
    {
    }
}