using System;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Text;
using Nevermore.Mapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nevermore.Advanced.Serialization
{
    public interface IDocumentSerializer
    {
        public Encoding EncodingForCompressedText { get; set; }
        
        object SerializeCompressed(object instance, DocumentMap map);
        string SerializeText(object instance, DocumentMap map);

        object DeserializeSmallText(string text, Type type);
        object DeserializeLargeText(Stream dataReaderStream, Type type);
        object DeserializeCompressed(Stream dataReaderStream, Type type);
    }

    public static class ConfigurationExtensions
    {
        public static void UseJsonNetSerialization(this IRelationalStoreConfiguration configuration, Action<JsonSerializerSettings> callback)
        {
            if (!(configuration.Serializer is NewtonsoftDocumentSerializer jsonNet))
            {
                configuration.Serializer = jsonNet = new NewtonsoftDocumentSerializer(configuration.Mappings);
            }

            callback(jsonNet.SerializerSettings);
        }
    }

    public class NewtonsoftDocumentSerializer : IDocumentSerializer
    {
        public NewtonsoftDocumentSerializer(IDocumentMapRegistry mappings)
        {
            var contractResolver = new RelationalJsonContractResolver(mappings);
            SerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            };

            SerializerSettings.Converters.Add(new StringEnumConverter());
            EncodingForCompressedText = new UnicodeEncoding(false, false);
        }

        public JsonSerializerSettings SerializerSettings { get; }

        public Encoding EncodingForCompressedText { get; set; }

        public object SerializeCompressed(object instance, DocumentMap map)
        {
            // TODO: Use Bing RecycledMemoryStreams
            // The MemoryStream is not disposed/not in a using intentionally, since it will be sent to a DbParameter.
            var ms = new MemoryStream();
            using (var gz = new GZipStream(ms, CompressionLevel.Optimal, true))
            {
                using var buf = new BufferedStream(gz, 16384);
                using var writer = new StreamWriter(buf, EncodingForCompressedText);
                var serializer = JsonSerializer.Create(SerializerSettings);
                serializer.Serialize(writer, instance);
            }

            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        public string SerializeText(object instance, DocumentMap map)
        {
            return JsonConvert.SerializeObject(instance, SerializerSettings);
        }

        public object DeserializeSmallText(string text, Type type)
        {
            using var jsonTextReader = new JsonTextReader(new StringReader(text));
            var serializer = JsonSerializer.Create(SerializerSettings);
            return serializer.Deserialize(jsonTextReader, type);
        }

        public object DeserializeLargeText(Stream dataReaderStream, Type type)
        {
            using var jsonTextReader =
                new JsonTextReader(new StreamReader(dataReaderStream, Encoding.UTF8, false, 1024));
            var serializer = JsonSerializer.Create(SerializerSettings);
            return serializer.Deserialize(jsonTextReader, type);
        }

        public object DeserializeCompressed(Stream dataReaderStream, Type type)
        {
            using var gzip = new GZipStream(dataReaderStream, CompressionMode.Decompress);
            using var jsonTextReader =
                new JsonTextReader(new StreamReader(gzip, EncodingForCompressedText, false, 1024));
            var serializer = JsonSerializer.Create(SerializerSettings);
            return serializer.Deserialize(jsonTextReader, type);
        }
    }
}