using System;
using System.Collections.Generic;
using System.Reflection;
using Nevermore.Mapping;

namespace Nevermore.Serialization
{
    abstract class InheritedClassConverter<TDocument, TDiscriminator> : InheritedClassConverterBase<TDocument, TDiscriminator>
    {
        protected InheritedClassConverter(RelationalMappings relationalMappings) : base(relationalMappings)
        {
        }

        protected override TypeInfo GetTypeInfoFromDerivedType(string derivedType)
        {
            var type = typeof(TDiscriminator);

            if (type.IsEnum)
            {
                var enumType = (TDiscriminator) Enum.Parse(type, derivedType);
                if (!DerivedTypeMappings.ContainsKey(enumType))
                {
                    throw new Exception(
                        $"Unable to determine type to deserialize. {TypeDesignatingPropertyName} `{enumType}` does not map to a known type");
                }

                return DerivedTypeMappings[enumType].GetTypeInfo();
            }

            if (type == typeof(string))
            {
                var mappings = (Dictionary<string, Type>) DerivedTypeMappings;
                if (!mappings.ContainsKey(derivedType))
                {
                    throw new Exception($"Unable to determine type to deserialize. {TypeDesignatingPropertyName} `{derivedType}` does not map to a known type");
                }

                return mappings[derivedType].GetTypeInfo();
            }

            throw new Exception($"Unable to determine type to deserialize, override GetTypeInfoFromDerivedType to map the derivedType");
        }
    }

    abstract class InheritedClassConverter<TModel> : InheritedClassConverter<TModel, string>
    {
        protected InheritedClassConverter(RelationalMappings relationalMappings = null) : base(relationalMappings)
        {
        }
    }
}