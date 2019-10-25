using System;
using System.Collections.Generic;
using System.Reflection;
using Nevermore.Contracts;
using Newtonsoft.Json;

namespace Nevermore.Serialization
{
    public abstract class ExtensibleEnumConverter<T> : JsonConverter
        where T : ExtensibleEnum
    {
        protected abstract IDictionary<string, T> Mappings { get; }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                var enumValue = value as T;
                writer.WriteValue(enumValue.Name);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            
            var readerValue = (string)reader.Value;
            if (!Mappings.ContainsKey(readerValue))
            {
                throw new InvalidOperationException($"Unknown {typeof(T)} '{readerValue}'");                
            }
            return Mappings[readerValue];
        }
        
        public override bool CanConvert(Type objectType)
        {
            return typeof(T).GetTypeInfo().IsAssignableFrom(objectType);
        }
    }
}