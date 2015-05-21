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
            DocumentMap mapping;
            if (!mappings.TryGetValue(type, out mapping))
            {
                throw new KeyNotFoundException(string.Format("A mapping for the type '{0}' has not been defined", type.Name));
            }
            return mapping;
        }
    }
}