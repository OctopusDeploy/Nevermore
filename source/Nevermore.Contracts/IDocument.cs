namespace Nevermore.Contracts
{
    public interface IDocument<out T> : IId<T>, INamed
    {
    }

    public interface IDocument : IDocument<string>
    {
    }
}