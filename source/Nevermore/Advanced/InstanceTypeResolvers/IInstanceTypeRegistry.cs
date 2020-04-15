using System;

namespace Nevermore.Advanced.InstanceTypeResolvers
{
    /// <summary>
    /// Keeps a list of instance types registered against this store.
    /// </summary>
    public interface IInstanceTypeRegistry
    {
        /// <summary>
        /// Registers an <see cref="IInstanceTypeResolver"/> with this registry.
        /// </summary>
        /// <param name="resolver"></param>
        void Register(IInstanceTypeResolver resolver);
        Type Resolve(Type baseType, object typeColumnValue);
    }
}