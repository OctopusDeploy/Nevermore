using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.IO;
using Nevermore.Mapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nevermore.Advanced.Serialization
{
    public class NewtonsoftDocumentSerializer : IDocumentSerializer
    {
        static readonly RecyclableMemoryStreamManager MemoryStreamManager = new(1024 * 32, 1024 * 256, 1024 * 1024 * 2);
        readonly ArrayPoolAdapter arrayPoolAdapter = new();

        public NewtonsoftDocumentSerializer(IRelationalStoreConfiguration configuration)
        {
            var contractResolver = new RelationalJsonContractResolver(configuration);
            SerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            };

            SerializerSettings.Converters.Add(new StringEnumConverter());
            EncodingForCompressedText = new UnicodeEncoding(false, false);
            CompressionLevel = CompressionLevel.Optimal;
        }

        public JsonSerializerSettings SerializerSettings { get; }

        public Encoding EncodingForCompressedText { get; set; }
        public CompressionLevel CompressionLevel { get; set; }

        public Stream SerializeCompressed(object instance, DocumentMap map)
        {
            // The MemoryStream is not disposed/not in a using intentionally, since it will be sent to a DbParameter.
            // CommandExecutor disposes all parameters at the end of the execution.
            var memoryStream = MemoryStreamManager.GetStream("JsonNetSerializer:SerializeCompressed:" + map.Type.Name);
            using (var gz = new GZipStream(memoryStream, CompressionLevel, true))
            {
                // Buffer values before we GZIP them, this will help the compression
                using var buf = new BufferedStream(gz, 16384);
                using var writer = new StreamWriter(buf, EncodingForCompressedText);
                
                var serializer = JsonSerializer.Create(SerializerSettings);
                serializer.Serialize(writer, instance);
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        public TextReader SerializeText(object instance, DocumentMap map)
        { 
            if (!map.ExpectLargeDocuments)
            {
                // Serializing directly to a string is faster for small documents
                var text = JsonConvert.SerializeObject(instance, SerializerSettings);
                if (text.Length > NevermoreDefaults.LargeDocumentCutoffSize)
                    map.ExpectLargeDocuments = true;
                return new StringReader(text);
            }

            // The MemoryStream is not disposed/not in a using intentionally, since it will be sent to a DbParameter.
            // CommandExecutor disposes all parameters at the end of the execution.
            var memoryStream = MemoryStreamManager.GetStream("JsonNetSerializer:SerializeText:" + map.Type.Name);
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, 2048, true))
            {
                var serializer = JsonSerializer.Create(SerializerSettings);
                using var jsonTextWriter = new JsonTextWriter(writer);
                jsonTextWriter.ArrayPool = arrayPoolAdapter;
                serializer.Serialize(jsonTextWriter, instance);
            }
                
            memoryStream.Seek(0, SeekOrigin.Begin);
            return new StreamReader(memoryStream, Encoding.UTF8);
        }

        public object DeserializeSmallText(string text, Type type)
        {
            using var jsonTextReader = new JsonTextReader(new StringReader(text));
            jsonTextReader.ArrayPool = arrayPoolAdapter;
            var serializer = JsonSerializer.Create(SerializerSettings);
            return serializer.Deserialize(jsonTextReader, type);
        }

        public object DeserializeLargeText(TextReader reader, Type type)
        {
            using var jsonTextReader = new JsonTextReader(reader);
            jsonTextReader.ArrayPool = arrayPoolAdapter;
            var serializer = JsonSerializer.Create(SerializerSettings);
            return serializer.Deserialize(jsonTextReader, type);
        }

        public object DeserializeCompressed(Stream dataReaderStream, Type type)
        {
            using var gzip = new GZipStream(dataReaderStream, CompressionMode.Decompress);
            using var jsonTextReader = new JsonTextReader(new StreamReader(gzip, EncodingForCompressedText, false, 2048));
            jsonTextReader.ArrayPool = arrayPoolAdapter;
            var serializer = JsonSerializer.Create(SerializerSettings);
            return serializer.Deserialize(jsonTextReader, type);
        }
    }
}