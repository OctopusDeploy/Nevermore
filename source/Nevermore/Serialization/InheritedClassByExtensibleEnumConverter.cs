using System;
using System.Reflection;
using Nevermore.Contracts;
using Nevermore.Mapping;
using Newtonsoft.Json.Linq;

namespace Nevermore.Serialization
{
    public abstract class InheritedClassByExtensibleEnumConverter<TDocument, TDiscriminator> : InheritedClassConverterBase<TDocument, string>
        where TDiscriminator : ExtensibleEnum
    {
        protected InheritedClassByExtensibleEnumConverter(RelationalMappings relationalMappings = null) : base(relationalMappings)
        {
        }

        protected override string GetDesignatingValue(JToken designatingProperty)
        {
            foreach (var property in designatingProperty)
            {
                if (property.Type == JTokenType.Property &&
                    ((JProperty)property).Name == "Name")
                {
                    return ((JProperty)property).Value.ToString();
                }
            }
            return String.Empty;
        }

        protected override TypeInfo GetTypeInfoFromDerivedType(string derivedType)
        {
            if (!DerivedTypeMappings.ContainsKey(derivedType))
            {
                throw new Exception($"Unable to determine type to deserialize. {TypeDesignatingPropertyName} `{derivedType}` does not map to a known type");
            }

            var typeInfo = DerivedTypeMappings[derivedType].GetTypeInfo();
            return typeInfo;
        }
    }
}