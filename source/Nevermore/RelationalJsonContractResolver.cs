using System;
using System.Collections.Generic;
using System.Reflection;
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

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var members = GetSerializableMembers(type);
            if (members is null)
            {
                throw new JsonSerializationException("Null collection of serializable members returned.");
            }

            configuration.DocumentMaps.ResolveOptional(type, out var map);

            var properties = new JsonPropertyCollection(type);
            foreach (var member in members)
            {
                var property = CreateProperty(member, memberSerialization);

                // ID properties are stored as columns
                if (map?.IdColumn?.ColumnName == member.Name)
                {
                    property.Ignored = true;
                }

                // Indexed properties are stored as columns
                if (map?.Columns.Any(c => c?.Property.Name == member.Name) ?? false)
                {
                    property.Ignored = true;
                }

                if (!property.Writable && member is PropertyInfo propertyMember)
                {
                    var hasPrivateSetter = propertyMember.GetSetMethod(true) is not null;
                    property.Writable = hasPrivateSetter;
                }

                properties.AddProperty(property);
            }

            var orderedProperties = properties.OrderBy(p => p.Order ?? -1).ToList();
            return orderedProperties;
        }
    }
}
