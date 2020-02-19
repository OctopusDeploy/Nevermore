using Octopus.TinyTypes;

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

    /// <summary>
    /// Implementations must have a constructor that takes a single string value
    /// <see cref="Nevermore.Mapping.AmazingConverter"/>
    /// </summary>
    public interface IIdWrapper : ITinyType<string>
    {
    }
}