#nullable enable
using System;

namespace Nevermore.Advanced.TypeHandlers
{
    public interface ITypeHandlerRegistry
    {
        ITypeHandler? Resolve(Type type);
        void Register(ITypeHandler handler);
    }
}