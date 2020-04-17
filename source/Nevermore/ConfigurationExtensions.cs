using System;
using Nevermore.Advanced.Serialization;
using Newtonsoft.Json;

namespace Nevermore
{
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
}