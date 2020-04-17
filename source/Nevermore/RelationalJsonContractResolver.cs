using System;
using System.Reflection;
using Nevermore.Mapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;

namespace Nevermore
{
    public class RelationalJsonContractResolver : DefaultContractResolver
    {
        private IDocumentMapRegistry mappings;

        public RelationalJsonContractResolver(IDocumentMapRegistry mappings)
        {
            this.mappings = mappings;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            DocumentMap map;
            mappings.ResolveOptional(member.DeclaringType, out map);

            var property = base.CreateProperty(member, memberSerialization);

            // ID properties are stored as columns
            if (property.PropertyName == "Id" && map != null)
            {
                property.Ignored = true;
            }

            // Indexed properties are stored as columns
            if (map != null && map.IndexedColumns.Any(c => c.Property != null && c.Property.Name == member.Name))
            {
                property.Ignored = true;
            }

            if (!property.Writable)
            {
                var property2 = member as PropertyInfo;
                if (property2 != null)
                {
                    var hasPrivateSetter = property2.GetSetMethod(true) != null;
                    property.Writable = hasPrivateSetter;
                }
            }

            return property;
        }
    }
}
