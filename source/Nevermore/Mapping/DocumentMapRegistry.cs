using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nevermore.Contracts;

namespace Nevermore.Mapping
{
    public class DocumentMapRegistry : IDocumentMapRegistry
    {
        readonly ConcurrentDictionary<Type, DocumentMap> mappings = new ConcurrentDictionary<Type, DocumentMap>();

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

        public DocumentMap Resolve<TDocument>() where TDocument : IId
        {
            return Resolve(typeof(TDocument));
        }

        public DocumentMap Resolve(object instance)
        {
            var mapping = Resolve(instance.GetType());
            
            // Make sure we got the right one if the mapping defines a different resolver
            var mType = mapping.InstanceTypeResolver.GetTypeFromInstance(instance);
            return Resolve(mType);
        }
        
        public DocumentMap Resolve(Type type)
        {
            if (!ResolveOptional(type, out var mapping))
            {
                throw new KeyNotFoundException($"The type '{type.Name}' is a document, but a mapping has not been defined");
            }
            
            return mapping;
        }
    }
}