using System;
using System.Collections.Generic;
using System.Linq;
using Nevermore.Mapping;
using Nevermore.Serialization;
using Newtonsoft.Json;

namespace Nevermore
{
    public class RelationalStoreConfiguration
    {
        readonly JsonSerializerSettings jsonSettings;
        readonly RelationalMappings relationalMappings;

        public RelationalStoreConfiguration()
        {
            DatabaseValueConverter = new DatabaseValueConverter(this);
            
            relationalMappings = new RelationalMappings();
            
            jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new RelationalJsonContractResolver(relationalMappings),
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
            };
        }

        internal IRelationalMappings RelationalMappings => relationalMappings;

        List<CustomTypeSerialization> CustomTypeSerializations { get; } = new List<CustomTypeSerialization>();

        internal IDatabaseValueConverter DatabaseValueConverter { get; }

        /// <summary>
        /// Use this method to include any additional JsonSerializationSettings that are required. 
        /// </summary>
        /// <param name="callback">Callback action that will configure settings.</param>
        public void ConfigureJsonSerializationSettings(Action<JsonSerializerSettings> callback)
        {
            callback(jsonSettings);
        }

        void AddCustomTypeSerialization(CustomTypeSerializationBase customTypeSerialization)
        {
            if (customTypeSerialization is IInheritedClassSerialization customInheritedType)
            {
                // this is a custom type hierarchy that may be a row, a column, or a value in the JSON column
                jsonSettings.Converters.Add(customInheritedType.GetJsonConverter(this.relationalMappings));
            }
            else if (customTypeSerialization is CustomTypeSerialization customType)
            {
                // this is a custom type that may be a column or a value in the JSON column
                var jsonConverter = new CustomTypeConverter(customType);
                jsonSettings.Converters.Add(jsonConverter);

                CustomTypeSerializations.Add(customType);
            }
        }
        
        public void Initialize(IEnumerable<DocumentMap> documentMaps, IEnumerable<CustomTypeSerializationBase> customTypeDefinitions = null)
        {
            if (documentMaps == null)
                throw new ArgumentException("DocumentMaps must be specified", nameof(documentMaps));

            if (customTypeDefinitions != null)
            {
                foreach (var customTypeDefinition in customTypeDefinitions)
                {
                    AddCustomTypeSerialization(customTypeDefinition);
                }
            }

            relationalMappings.Install(documentMaps);

            foreach (var documentMap in documentMaps)
            {
                // DocumentMap doesn't enforce IId on the document type, so the IdColumn can be null if the document
                // doesn't have an Id property 
                documentMap.IdColumn?.Initialize(this);

                foreach (var column in documentMap.IndexedColumns)
                {
                    column.Initialize(this);
                }
            }
        }

        internal bool TryGetCustomTypeDefinitionForType(Type type, out CustomTypeSerialization customTypeSerialization)
        {
            customTypeSerialization = null;

            var definition = CustomTypeSerializations.FirstOrDefault(d => d.CanConvertType(type));
            if (definition == null) 
                return false;
            
            customTypeSerialization = definition;
            return true;

        }
        
        internal string SerializeObject(object value, Type type)
        {
            return JsonConvert.SerializeObject(value, type, jsonSettings);
        }
        internal object DeserializeObject(string value, Type type)
        {
            return JsonConvert.DeserializeObject(value, type, jsonSettings);
        }
    }
}