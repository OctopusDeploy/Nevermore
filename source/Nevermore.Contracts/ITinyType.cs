namespace Nevermore.Contracts
{
    public interface ITinyType<out T>
    {
        T Value { get; }
    }
}