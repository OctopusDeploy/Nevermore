using System;
using System.Collections.Generic;
using Nevermore.Serialization;
using Newtonsoft.Json;

namespace Nevermore.Mapping
{
    /// <summary>
    /// This type is used to store objects from an inheritance hierarchy.
    /// </summary>
    /// <typeparam name="TModelBase"></typeparam>
    public abstract class TypeDesignatingTypeDefinition<TModelBase> : TypeDesignatingTypeDefinition<TModelBase, string>
    {
    }

    public abstract class TypeDesignatingTypeDefinition<TModelBase, TDiscriminator> : CustomTypeDefinition, ITypeDesignatingTypeDefinition
    {
        public override bool CanConvertType(Type type)
        {
            return typeof(TModelBase).IsAssignableFrom(type);
        }

        protected abstract IDictionary<TDiscriminator, Type> DerivedTypeMappings { get; }
        protected abstract string TypeDesignatingPropertyName { get; }

        public override object ToDbValue(object instance, bool isJson)
        {
            return JsonConvert.SerializeObject(instance);
        }

        public override object FromDbValue(object value, Type targetType)
        {
            var obj = JsonConvert.DeserializeObject((string)value, targetType);
            return obj;
        }

        public JsonConverter GetJsonConverter(RelationalMappings relationalMappings)
        {
            return new CustomInheritedTypeClassConverter(() => DerivedTypeMappings, () => TypeDesignatingPropertyName, relationalMappings);
        }

        class CustomInheritedTypeClassConverter : InheritedClassConverter<TModelBase, TDiscriminator>
        {
            readonly Func<IDictionary<TDiscriminator, Type>> derivedTypeMappings;
            readonly Func<string> typeDesignatingPropertyName;

            public CustomInheritedTypeClassConverter(Func<IDictionary<TDiscriminator, Type>> derivedTypeMappings, Func<string> typeDesignatingPropertyName, RelationalMappings relationalMappings) : base(relationalMappings)
            {
                this.derivedTypeMappings = derivedTypeMappings;
                this.typeDesignatingPropertyName = typeDesignatingPropertyName;
            }

            protected override IDictionary<TDiscriminator, Type> DerivedTypeMappings => derivedTypeMappings();
            protected override string TypeDesignatingPropertyName => typeDesignatingPropertyName();
        }

    }
}