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
        void Register(IPrimaryKeyHandler strategy);

        IPrimaryKeyHandler? Resolve(DocumentMap documentMap);
    }

    class PrimaryKeyHandlerRegistry : IPrimaryKeyHandlerRegistry
    {
        // maps key type to IPrimaryKeyHandler concrete type
        readonly ConcurrentDictionary<Type, IPrimaryKeyHandler> mappings = new ConcurrentDictionary<Type, IPrimaryKeyHandler>();

        public void Register(IPrimaryKeyHandler handler)
        {
            mappings[handler.Type] = handler;
        }

        public IPrimaryKeyHandler? Resolve(DocumentMap documentMap)
        {
            if (!(documentMap.PrimaryKeyHandler is null))
                return documentMap.PrimaryKeyHandler;

            if (documentMap.IdColumn is null)
                throw new InvalidOperationException($"Map for document type {documentMap.Type.Name} does not specify an Id column.");

            var idType = documentMap.IdColumn.Type;

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