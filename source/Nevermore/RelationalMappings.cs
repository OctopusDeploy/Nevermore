using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Nevermore
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
            return mappings.TryGetValue(type, out map);
        }

        public DocumentMap Get(Type type)
        {
            DocumentMap mapping = null;

            // Walk up the inheritance chain until we find a mapping
            var currentType = type;
            while (currentType != null && !mappings.TryGetValue(currentType, out mapping))
            {
                currentType = currentType.BaseType;
            }

            if (mapping == null)
            {
                throw new KeyNotFoundException(string.Format("A mapping for the type '{0}' has not been defined", type.Name));
            }

            return mapping;
        }
    }
}