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
            if (members == null)
            {
                throw new JsonSerializationException("Null collection of serializable members returned.");
            }

            var properties = new JsonPropertyCollection(type);

            foreach (var member in members)
            {
                var property = CreateProperty(type, member, memberSerialization);

                if (property != null)
                {
                    properties.AddProperty(property);
                }
            }

            var orderedProperties = properties.OrderBy(p => p.Order ?? -1).ToList();
            return orderedProperties;
        }

        JsonProperty CreateProperty(Type type, MemberInfo member, MemberSerialization memberSerialization)
        {
            // Todo: Newtonsoft derives the MemberInfo from the base class if that's where the member is defined,
            //  so ReflectedType will always be the same as DeclaredType. Issue currently open to fix this:
            //  https://github.com/JamesNK/Newtonsoft.Json/issues/2488
            // configuration.DocumentMaps.ResolveOptional(member.ReflectedType, out var map);
            configuration.DocumentMaps.ResolveOptional(type, out var map);

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
