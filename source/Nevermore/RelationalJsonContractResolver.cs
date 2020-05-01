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
        readonly IRelationalStoreConfiguration configuration;

        public RelationalJsonContractResolver(IRelationalStoreConfiguration configuration)
        {
            this.configuration = configuration;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            configuration.DocumentMaps.ResolveOptional(member.DeclaringType, out var map);

            var property = base.CreateProperty(member, memberSerialization);

            // ID properties are stored as columns
            if (property.PropertyName == "Id" && map != null)
            {
                property.Ignored = true;
            }

            // Indexed properties are stored as columns
            if (map != null && map.Columns.Any(c => c.Property != null && c.Property.Name == member.Name))
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
