#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nevermore.Mapping
{
    public class DocumentMapRegistry : IDocumentMapRegistry
    {
        readonly IPrimaryKeyHandlerRegistry primaryKeyHandlerRegistry;
        readonly ConcurrentDictionary<Type, DocumentMap> mappings = new ConcurrentDictionary<Type, DocumentMap>();
        readonly ConcurrentDictionary<string, List<string>> mappingColumnNamesSortedWithJsonLastCache = new();

        public DocumentMapRegistry(IPrimaryKeyHandlerRegistry primaryKeyHandlerRegistry)
        {
            this.primaryKeyHandlerRegistry = primaryKeyHandlerRegistry;
        }

        public List<DocumentMap> GetAll()
        {
            return new List<DocumentMap>(mappings.Values);
        }

        public void Register(DocumentMap map)
        {
            map.Validate();
            mappings[map.Type] = map;
        }

        public void Register(IDocumentMap map)
        {
            Register(new List<IDocumentMap> { map });
        }

        public void Register(params IDocumentMap[] mappingsToAdd)
        {
            Register(mappingsToAdd.AsEnumerable());
        }

        public void Register(IEnumerable<IDocumentMap> mappingsToAdd)
        {
            foreach (var mapping in mappingsToAdd)
            {
                var map = mapping.Build(primaryKeyHandlerRegistry);
                Register(map);

                foreach (var childMap in map.ChildTables)
                {
                    Register(childMap.Build(primaryKeyHandlerRegistry));
                }
            }
        }

        public bool ResolveOptional(Type type, out DocumentMap map)
        {
            var maps = new List<DocumentMap>();

            // Walk up the inheritance chain and make sure there's only one map for the document.
            var currentType = type;

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
                throw new InvalidOperationException($"More than one document map is registered against the type '{type.FullName}'. The following maps could apply: " + string.Join(", ", maps.Select(m => m.GetType().FullName)));

            map = maps.SingleOrDefault();
            return map != null;
        }

        public DocumentMap Resolve<TDocument>()
        {
            return Resolve(typeof(TDocument));
        }

        public DocumentMap Resolve(object instance)
        {
            var mapping = Resolve(instance.GetType());
            return mapping;
        }

        public DocumentMap Resolve(Type type)
        {
            if (!ResolveOptional(type, out var mapping))
            {
                throw NotRegistered(type);
            }

            return mapping;
        }

        public object? GetId(object instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();
            if (!ResolveOptional(type, out var map))
                throw NotRegistered(type);

            return map.GetId(instance);
        }

        static Exception NotRegistered(Type type)
        {
            return new InvalidOperationException($"To be used for this operation, the class '{type.FullName}' must have a document map that is registered with this relational store. Types without a document map cannot be used for this operation.");
        }
    }
}