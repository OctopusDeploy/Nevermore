using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nevermore.Mapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nevermore.Serialization
{
    public abstract class InheritedClassConverter<TDocument, TDiscriminator> : JsonConverter
    {
        readonly RelationalMappings relationalMappings;

        protected InheritedClassConverter(RelationalMappings relationalMappings)
        {
            this.relationalMappings = relationalMappings;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            var documentType = value.GetType().GetTypeInfo();

            var map = relationalMappings.Get(documentType);

            foreach (var property in documentType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty)
                .Where(p => p.Name == TypeDesignatingPropertyName || 
                            (p.CanRead && p.Name != map.IdColumn.Property.Name && map.IndexedColumns.All(c => p.Name != c.Property.Name))))
            {
                writer.WritePropertyName(property.Name);
                serializer.Serialize(writer, property.GetValue(value, null));
            }

            WriteTypeProperty(writer, value, serializer);

            writer.WriteEndObject();
        }

        protected virtual void WriteTypeProperty(JsonWriter writer, object value, JsonSerializer serializer)
        { }

        protected virtual Type DefaultType { get; } = null;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var jo = JObject.Load(reader);
            var designatingProperty = jo.GetValue(TypeDesignatingPropertyName);
            TypeInfo typeInfo;
            if (designatingProperty == null)
            {
                if (DefaultType == null)
                {
                    throw new Exception($"Unable to determine type to deserialize. Missing property `{TypeDesignatingPropertyName}`");
                }
                typeInfo = DefaultType.GetTypeInfo();
            }
            else
            {
                var derivedType = designatingProperty.ToObject<string>();
                typeInfo = GetTypeInfoFromDerivedType(derivedType);
            }

            var ctor = typeInfo.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
            var args = ctor.GetParameters().Select(p =>
                jo.GetValue(char.ToUpper(p.Name[0]) + p.Name.Substring(1))
                    .ToObject(p.ParameterType, serializer)).ToArray();
            var instance = ctor.Invoke(args);
            foreach (var prop in typeInfo
                .GetProperties(BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.Instance)
                .Where(p => p.CanWrite))
            {
                var val = jo.GetValue(prop.Name);
                if (val != null)
                    prop.SetValue(instance, val.ToObject(prop.PropertyType, serializer), null);
            }
            return instance;
        }

        protected virtual TypeInfo GetTypeInfoFromDerivedType(string derivedType)
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

        public override bool CanConvert(Type objectType)
        {
            return typeof(TDocument).GetTypeInfo().IsAssignableFrom(objectType);
        }

        protected abstract IDictionary<TDiscriminator, Type> DerivedTypeMappings { get; }

        protected abstract string TypeDesignatingPropertyName { get; }
    }

    public abstract class InheritedClassConverter<TModel> : InheritedClassConverter<TModel, string>
    {
        protected InheritedClassConverter(RelationalMappings relationalMappings) : base(relationalMappings)
        {
        }

        protected override TypeInfo GetTypeInfoFromDerivedType(string derivedType)
        {
            if (!DerivedTypeMappings.ContainsKey(derivedType))
            {
                throw new Exception($"Unable to determine type to deserialize. {TypeDesignatingPropertyName} does not map to a known type");
            }

            var typeInfo = DerivedTypeMappings[derivedType].GetTypeInfo();
            return typeInfo;
        }
    }
}