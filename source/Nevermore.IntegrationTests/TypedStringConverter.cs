using System;
using System.Reflection;
using Newtonsoft.Json;

namespace Nevermore.IntegrationTests
{
    public class TypedStringConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRaw(((TypedString) value).Value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
                throw new JsonSerializationException(
                    $"Unexpected token or value when parsing TypedString. Token: {reader.TokenType}, Value: {reader.Value}");
            return Activator.CreateInstance(objectType, (string) reader.Value);
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(TimeSpan).GetTypeInfo().IsAssignableFrom(objectType);
        }
    }
}