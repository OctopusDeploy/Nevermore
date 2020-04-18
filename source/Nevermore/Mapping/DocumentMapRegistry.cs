using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nevermore.Querying.AST;

namespace Nevermore.Mapping
{
    public class DocumentMapRegistry : IDocumentMapRegistry
    {
        readonly ConcurrentDictionary<Type, DocumentMap> mappings = new ConcurrentDictionary<Type, DocumentMap>();
        readonly ConcurrentDictionary<Type, Func<object, string>> idReaders = new ConcurrentDictionary<Type, Func<object, string>>();

        public List<DocumentMap> GetAll()
        {
            return new List<DocumentMap>(mappings.Values);
        }

        public void Register(DocumentMap map)
        {
            Register(new List<DocumentMap> { map });
        }

        public void Register(params DocumentMap[] mappingsToAdd)
        {
            Register(mappingsToAdd.AsEnumerable());
        }
        
        public void Register(IEnumerable<DocumentMap> mappingsToAdd)
        {
            foreach (var mapping in mappingsToAdd)
            {
                mappings[mapping.Type] = mapping;
            }
        }

        public bool ResolveOptional(Type type, out DocumentMap map)
        {
            DocumentMap mapping = null;

            // Walk up the inheritance chain until we find a mapping
            var currentType = type;
            while (currentType != null && !mappings.TryGetValue(currentType, out mapping))
            {
                currentType = currentType.GetTypeInfo().BaseType;
            }

            map = mapping;

            return mapping != null;
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
        
        public string GetId(object instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();
            var reader = idReaders.GetOrAdd(type, CreateIdReader);
            return reader(instance);
        }

        Func<object, string> CreateIdReader(Type type)
        {
            if (!ResolveOptional(type, out var map))
                throw NotRegistered(type);

            var readerWriter = map.IdColumn.PropertyHandler;
            return inst => (string)readerWriter.Read(inst);
        }

        static Exception NotRegistered(Type type)
        {
            return new InvalidOperationException($"To be used for this operation, the class '{type.FullName}' must have a document map that is registered with this relational store. Types without a document map cannot be used for this operation.");
        }
    }
}