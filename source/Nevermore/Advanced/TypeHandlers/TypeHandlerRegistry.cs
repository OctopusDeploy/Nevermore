#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Nevermore.Advanced.TypeHandlers
{
    public class TypeHandlerRegistry : ITypeHandlerRegistry
    {
        readonly List<ITypeHandler> typeHandlers = new List<ITypeHandler>();
        readonly ConcurrentDictionary<Type, ITypeHandler> cache = new ConcurrentDictionary<Type, ITypeHandler>();

        public ITypeHandler? Resolve(Type type)
        {
            if (cache.TryGetValue(type, out var existing))
                return existing;

            var handler = typeHandlers.OrderBy(o => o.Priority).FirstOrDefault(h => h.CanConvert(type));
            if (handler != null)
                // Only cache if we found a value
                cache.TryAdd(type, handler);
            return handler;
        }

        public void Register(ITypeHandler handler)
        {
            typeHandlers.Add(handler);
        }
    }
}