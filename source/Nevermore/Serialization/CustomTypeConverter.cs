using System;
using Nevermore.Mapping;
using Newtonsoft.Json;

namespace Nevermore.Serialization
{
    class CustomTypeConverter : JsonConverter
    {
        readonly CustomSingleTypeDefinition customTypeDefinition;

        public CustomTypeConverter(CustomSingleTypeDefinition customTypeDefinition)
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