using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Nevermore.Mapping
{
    public class RelationalMappings
    {
        readonly ConcurrentDictionary<Type, DocumentMap> mappings = new ConcurrentDictionary<Type, DocumentMap>();

        public List<DocumentMap> GetAll()
        {
            return new List<DocumentMap>(mappings.Values);
        }

        public void Install(IEnumerable<DocumentMap> mappingsToAdd)
        {
            foreach (var mapping in mappingsToAdd)
            {
                mappings[mapping.Type] = mapping;
            }
        }

        public bool TryGet(Type type, out DocumentMap map)
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

        public DocumentMap Get(object instance)
        {
            var mapping = Get(instance.GetType());
            
            // Make sure we got the right one if the mapping defines a different resolver
            var mType = mapping.InstanceTypeResolver.GetTypeFromInstance(instance);
            return Get(mType);
        }
        
        public DocumentMap Get(Type type)
        {
            if (!TryGet(type, out var mapping))
            {
                throw new KeyNotFoundException(string.Format("A mapping for the type '{0}' has not been defined", type.Name));
            }
            
            return mapping;
        }
    }
}