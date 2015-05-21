//using System;
//using System.Collections.Generic;
//using Newtonsoft.Json;

//namespace Nevermore
//{
//    public class EncryptedValueConverter : JsonConverter
//    {
//        readonly IMasterKeyEncryption encryption;

//        public EncryptedValueConverter(IMasterKeyEncryption encryption)
//        {
//            this.encryption = encryption;
//        }

//        public override bool CanRead
//        {
//            get { return true; }
//        }

//        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
//        {
//            if (value == null)
//            {
//                writer.WriteNull();
//                return;
//            }

//            var bytes = value as byte[];
//            if (bytes != null)
//            {
//                writer.WriteValue(encryption.ToCiphertext(bytes).ToBase64());
//                return;
//            }

//            var text = value as string;
//            if (text != null)
//            {
//                writer.WriteValue(encryption.StringToCiphertext(text).ToBase64());
//                return;
//            }

//            var array = value as string[];
//            if (array != null)
//            {
//                writer.WriteStartArray();
//                foreach (var element in array)
//                {
//                    writer.WriteValue(encryption.StringToCiphertext(element).ToBase64());
//                }
//                writer.WriteEnd();
//                return;
//            }

//            throw new NotSupportedException(string.Format("The type '{0}' cannot be encrypted: unable to write value: {1}", value.GetType(), value));
//        }

//        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
//        {
//            if (reader.TokenType == JsonToken.Null)
//                return null;

//            if (objectType == typeof(string))
//                return encryption.ToPlaintextString(EncryptedBytes.FromBase64((string)reader.Value));

//            if (objectType == typeof(byte[]))
//                return encryption.ToPlaintext(EncryptedBytes.FromBase64((string)reader.Value));

//            if (objectType == typeof(string[]))
//            {
//                var items = new List<string>();
//                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
//                {
//                    items.Add(encryption.ToPlaintextString(EncryptedBytes.FromBase64((string)reader.Value)));
//                }
//                return items.ToArray();
//            }

//            throw new NotSupportedException(string.Format("The type '{0}' cannot be encrypted: unable to read value: {1}", objectType.FullName, reader.Value));
//        }

//        public override bool CanConvert(Type objectType)
//        {
//            return true;
//        }
//    }
//}