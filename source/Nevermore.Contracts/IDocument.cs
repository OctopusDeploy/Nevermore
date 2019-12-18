namespace Nevermore.Contracts
{
    public interface IDocument<out TId> : IId<TId>, INamed 
        where TId : IIdWrapper
    {
    }

    public interface IDocument : IDocument<LegacyStringId>, IId
    {
    }
}