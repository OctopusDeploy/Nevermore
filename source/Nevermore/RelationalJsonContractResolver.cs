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
        public RelationalJsonContractResolver()
        {
        }

        [Obsolete("Using this is no longer necessary, call the default constructor")]
        public RelationalJsonContractResolver(RelationalMappings mappings)
        {
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            
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
