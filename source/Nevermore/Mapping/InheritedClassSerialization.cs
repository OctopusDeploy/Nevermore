using System;
using System.Collections.Generic;
using System.Reflection;
using Nevermore.Serialization;
using Newtonsoft.Json;

namespace Nevermore.Mapping
{
    /// <summary>
    /// This type is used to store objects from an inheritance hierarchy.
    /// </summary>
    /// <typeparam name="TModelBase"></typeparam>
    public abstract class InheritedClassSerialization<TModelBase> : InheritedClassSerialization<TModelBase, string>
    {
    }

    public abstract class InheritedClassSerialization<TModelBase, TDiscriminator> : CustomTypeSerializationBase, IInheritedClassSerialization
    {
        public override bool CanConvertType(Type type)
        {
            return typeof(TModelBase).GetTypeInfo().IsAssignableFrom(type);
        }

        protected abstract IDictionary<TDiscriminator, Type> DerivedTypeMappings { get; }
        protected abstract string TypeDesignatingPropertyName { get; }

        internal override JsonConverter GetJsonConverter(RelationalMappings relationalMappings)
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