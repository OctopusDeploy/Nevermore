#nullable enable
using System;
using System.Collections.Generic;
using Nevermore.Advanced;
using Nevermore.Advanced.Serialization;
using Nevermore.Mapping;
using Newtonsoft.Json;

namespace Nevermore
{
    public static class ConfigurationExtensions
    {
        public static void UseJsonNetSerialization(this IRelationalStoreConfiguration configuration, Action<JsonSerializerSettings> callback)
        {
            if (!(configuration.DocumentSerializer is NewtonsoftDocumentSerializer jsonNet))
            {
                configuration.DocumentSerializer = jsonNet = new NewtonsoftDocumentSerializer(configuration);
            }

            callback(jsonNet.SerializerSettings);
        }

        internal static string GetSchemaNameOrDefault(this IRelationalStoreConfiguration configuration, string? schemaName)
        {
            return schemaName
                ?? configuration.DefaultSchema
                ?? NevermoreDefaults.FallbackDefaultSchemaName;
        }

        internal static string GetSchemaNameOrDefault(this IRelationalStoreConfiguration configuration, DocumentMap documentMap)
            => GetSchemaNameOrDefault(configuration, documentMap.SchemaName);

        internal static string GetSchemaNameOrDefault(this IRelationalStoreConfiguration configuration, RelatedDocumentsMapping relatedDocumentsMapping)
            => GetSchemaNameOrDefault(configuration, relatedDocumentsMapping.SchemaName);

    }
}