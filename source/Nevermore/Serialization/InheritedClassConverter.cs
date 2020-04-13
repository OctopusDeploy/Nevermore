using System;
using System.Reflection;
using Nevermore.Mapping;

namespace Nevermore.Serialization
{
    public abstract class InheritedClassConverter<TDocument, TDiscriminator> : InheritedClassConverterBase<TDocument, TDiscriminator>
        where TDiscriminator : struct
    {
        protected InheritedClassConverter(IDocumentMapRegistry documentMapRegistry = null) : base(documentMapRegistry)
        {
        }

        protected override TypeInfo GetTypeInfoFromDerivedType(string derivedType)
        {
            var enumType = (TDiscriminator) Enum.Parse(typeof(TDiscriminator), derivedType);
            if (!DerivedTypeMappings.ContainsKey(enumType))
            {
                throw new Exception(
                    $"Unable to determine type to deserialize. {TypeDesignatingPropertyName} `{enumType}` does not map to a known type");
            }

            var typeInfo = DerivedTypeMappings[enumType].GetTypeInfo();
            return typeInfo;
        }
    }

    public abstract class InheritedClassConverter<TModel> : InheritedClassConverterBase<TModel, string>
    {
        protected InheritedClassConverter(IDocumentMapRegistry documentMapRegistry = null) : base(documentMapRegistry)
        {
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