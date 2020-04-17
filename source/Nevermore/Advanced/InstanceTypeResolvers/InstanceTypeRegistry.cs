using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Nevermore.Advanced.InstanceTypeResolvers
{
    internal class InstanceTypeRegistry : IInstanceTypeRegistry
    {
        readonly List<IInstanceTypeResolver> resolvers = new List<IInstanceTypeResolver>();
        readonly ConcurrentDictionary<(Type, object), Type> cache = new ConcurrentDictionary<(Type, object), Type>();

        public void Register(IInstanceTypeResolver resolver)
        {
            resolvers.Add(resolver);
        }

        public Type Resolve(Type baseType, object typeColumnValue)
        {
            var key = (baseType, typeColumnValue);
            if (cache.TryGetValue(key, out var existing))
                return existing;

            var concreteType = Find(baseType, typeColumnValue);
            if (concreteType != null)
                // Only cache if we found a value
                cache.TryAdd(key, concreteType);
            return concreteType;
        }

        Type Find(Type baseType, object typeColumnValue)
        {
            foreach (var resolver in resolvers.OrderBy(r => r.Order))
            {
                var result = resolver.Resolve(baseType, typeColumnValue);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}