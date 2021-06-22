#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nevermore.Mapping
{
    public interface IPrimaryKeyHandlerRegistry
    {
        /// <summary>
        /// Registers a mapping of key type to PrimaryKeyHandler type. StringPrimaryKeyHandler, IntPrimaryKeyHandler, LongPrimaryKeyHandler, and GuidPrimaryKeyHandler
        /// are registered by default, with default settings. Calling register again for them will overwrite the default, you can use this if you want to override a
        /// global default.
        /// </summary>
        void Register(IPrimaryKeyHandler handler);

        IPrimaryKeyHandler? Resolve(Type columnType);
    }

    class PrimaryKeyHandlerRegistry : IPrimaryKeyHandlerRegistry
    {
        // maps key type to IPrimaryKeyHandler concrete type
        readonly ConcurrentDictionary<Type, IPrimaryKeyHandler> mappings = new ConcurrentDictionary<Type, IPrimaryKeyHandler>();

        public PrimaryKeyHandlerRegistry()
        {
            mappings.TryAdd(typeof(string), new StringPrimaryKeyHandler());
            mappings.TryAdd(typeof(int), new IntPrimaryKeyHandler());
            mappings.TryAdd(typeof(long), new LongPrimaryKeyHandler());
            mappings.TryAdd(typeof(Guid), new GuidPrimaryKeyHandler());
        }

        public void Register(IPrimaryKeyHandler handler)
        {
            mappings[handler.Type] = handler;
        }

        public IPrimaryKeyHandler? Resolve(Type columnType)
        {
            var idType = columnType;

            var maps = new List<IPrimaryKeyHandler>();

            // Walk up the inheritance chain and make sure there's only one map for the document.
            var currentType = idType;

            while (true)
            {
                if (mappings.TryGetValue(currentType, out var m))
                {
                    maps.Add(m);
                }

                currentType = currentType.GetTypeInfo().BaseType;
                if (currentType == typeof(object) || currentType == null)
                    break;
            }

            if (maps.Count > 1)
                throw new InvalidOperationException($"More than one key allocation strategy is registered against the type '{idType.FullName}'. The following maps could apply: " + string.Join(", ", maps.Select(m => m.GetType().FullName)));

            var strategy = maps.SingleOrDefault();
            return strategy;
        }
    }
}