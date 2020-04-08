using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nevermore.Mapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nevermore.Serialization
{
    class CustomTypeConverter : JsonConverter
    {
        readonly ConcurrentDictionary<TypeInfo, IReadOnlyList<PropertyInfo>> unmappedReadablePropertiesCache = new ConcurrentDictionary<TypeInfo, IReadOnlyList<PropertyInfo>>();
        readonly ConcurrentDictionary<TypeInfo, IReadOnlyList<PropertyInfo>> writeablePropertiesCache = new ConcurrentDictionary<TypeInfo, IReadOnlyList<PropertyInfo>>();
        readonly CustomTypeSerialization customTypeSerialization;

        public CustomTypeConverter(CustomTypeSerialization customTypeSerialization)
        {
            this.customTypeSerialization = customTypeSerialization;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var obj = customTypeSerialization.ConvertToJsonColumnValue(value);
            if (obj != null)
            {
                writer.WriteValue(obj);
                return;
            }
            
            writer.WriteStartObject();

            var documentType = value.GetType().GetTypeInfo();

            var properties = unmappedReadablePropertiesCache.GetOrAdd(documentType, GetUnmappedReadableProperties);
            foreach (var property in properties)
            {
                writer.WritePropertyName(property.Name);
                serializer.Serialize(writer, GetPropertyValue(property, value));
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var obj = customTypeSerialization.ConvertFromJsonDbValue(reader.Value, objectType);
            if (obj != null)
            {
                return obj;
            }

            var jo = JObject.Load(reader);
            var typeInfo = objectType.GetTypeInfo();

            var ctor = typeInfo.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
            if (ctor == null)
            {
                throw new Exception($"Type {typeInfo.Name} must have a public constructor");
            }

            var args = ctor.GetParameters().Select(p =>
                jo.GetValue(char.ToUpper(p.Name[0]) + p.Name.Substring(1))?.ToObject(p.ParameterType, serializer)).ToArray();
            var instance = ctor.Invoke(args);
            
            var properties = writeablePropertiesCache.GetOrAdd(typeInfo, GetWritableProperties);
            foreach (var prop in properties)
            {
                var val = jo.GetValue(prop.Name);
                if (val != null)
                {
                    var value = val.ToObject(prop.PropertyType, serializer);
                    SetPropertyValue(prop, instance, value);
                }
            }
            return instance;
        }

        public override bool CanConvert(Type objectType)
        {
            return customTypeSerialization.CanConvertType(objectType);
        }
        
        IReadOnlyList<PropertyInfo> GetUnmappedReadableProperties(TypeInfo documentType)
            => documentType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty)
                .Where(p => p.CanRead && p.GetCustomAttribute(typeof(JsonIgnoreAttribute)) == null)
                .ToArray();

        IReadOnlyList<PropertyInfo> GetWritableProperties(TypeInfo type)
            => type
                .GetProperties(BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.GetCustomAttribute(typeof(JsonIgnoreAttribute)) == null)
                .ToArray();

        protected virtual object GetPropertyValue(PropertyInfo property, object instance)
        {
            return property.GetValue(instance, null);
        }
        protected virtual void SetPropertyValue(PropertyInfo prop, object instance, object value)
        {
            prop.SetValue(instance, value, null);
        }

    }
}