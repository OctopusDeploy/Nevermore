using System;
using Nevermore.Advanced.Serialization;
using Newtonsoft.Json;

namespace Nevermore
{
    public static class ConfigurationExtensions
    {
        public static void UseJsonNetSerialization(this IRelationalStoreConfiguration configuration, Action<JsonSerializerSettings> callback)
        {
            if (!(configuration.DocumentSerializer is NewtonsoftDocumentSerializer jsonNet))
            {
                configuration.DocumentSerializer = jsonNet = new NewtonsoftDocumentSerializer(configuration.DocumentMaps);
            }

            callback(jsonNet.SerializerSettings);
        }
    }
}