using System;

namespace Nevermore.Mapping
{
    public interface IPrimaryKeyHandler
    {
        Type Type { get; }
    }

    public interface IPrimaryKeyHandler<TKey> : IPrimaryKeyHandler
    {}
}