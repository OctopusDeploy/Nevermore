using System;
using Nevermore.Contracts;
using Newtonsoft.Json;

namespace Nevermore.Serialization
{
    public class CustomTypeConverter : JsonConverter
    {
        readonly ICustomTypeDefinition customTypeDefinition;

        public CustomTypeConverter(ICustomTypeDefinition customTypeDefinition)
        {
            this.customTypeDefinition = customTypeDefinition;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(customTypeDefinition.ToDbValue(value));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            return customTypeDefinition.FromDbValue(reader.Value);
        }

        public override bool CanConvert(Type objectType)
        {
            return customTypeDefinition.ModelType == objectType;
        }
    }
}