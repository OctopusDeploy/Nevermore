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

        public Type ResolveTypeFromValue(Type baseType, object typeColumnValue)
        {
            var key = (baseType, typeColumnValue);
            if (cache.TryGetValue(key, out var existing))
                return existing;

            var concreteType = FindTypeByValue(baseType, typeColumnValue);
            if (concreteType != null)
                // Only cache if we found a value
                cache.TryAdd(key, concreteType);
            return concreteType;
        }

        public object ResolveValueFromType(Type type)
        {
            // TODO: Caching
            return FindValueByType(type);
        }

        Type FindTypeByValue(Type baseType, object typeColumnValue)
        {
            return resolvers
                .OrderBy(r => r.Order)
                .Select(resolver => resolver.ResolveTypeFromValue(baseType, typeColumnValue))
                .FirstOrDefault(result => result != null);
        }

        object FindValueByType(Type type)
        {
            return resolvers
                .OrderBy(r => r.Order)
                .Select(resolver => resolver.ResolveValueFromType(type))
                .FirstOrDefault(result => result != null);
        }
    }
}