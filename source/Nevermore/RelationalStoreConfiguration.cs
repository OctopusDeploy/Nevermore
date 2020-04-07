using System;
using System.Collections.Generic;
using Nevermore.Mapping;
using Nevermore.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Nevermore
{
    public class RelationalStoreConfiguration
    {
        readonly JsonSerializerSettings jsonSettings;
        readonly RelationalMappings relationalMappings;

        public RelationalStoreConfiguration()
        {
            AmazingConverter = new AmazingConverter(this);
            
            relationalMappings = new RelationalMappings();
            
            jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new RelationalJsonContractResolver(relationalMappings),
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
            };
        }

        public IRelationalMappings RelationalMappings => relationalMappings;

        public Dictionary<Type, CustomSingleTypeDefinition> CustomSingleTypeDefinitions { get; } = new Dictionary<Type, CustomSingleTypeDefinition>();

        public IAmazingConverter AmazingConverter { get; }

        public void SetSerializationContractResolver<TResolver>(TResolver resolver)
            where TResolver : DefaultContractResolver
        {
            jsonSettings.ContractResolver = resolver;
        }

        public void SetSerializationTypeNameHandlingOptions(TypeNameHandling typeNameHandling, TypeNameAssemblyFormatHandling typeNameAssemblyFormatHandling)
        {
            jsonSettings.TypeNameHandling = typeNameHandling;
            jsonSettings.TypeNameAssemblyFormatHandling = typeNameAssemblyFormatHandling;
        }

        void AddCustomTypeDefinition(CustomTypeDefinition customTypeDefinition)
        {
            if (customTypeDefinition is CustomSingleTypeDefinition customType)
            {
                var jsonConverter = new CustomTypeConverter(customType);
                jsonSettings.Converters.Add(jsonConverter);

                CustomSingleTypeDefinitions.Add(customType.TypeToConvert, customType);
            }
            if (customTypeDefinition is ICustomInheritedTypeDefinition customInheritedType)
            {
                jsonSettings.Converters.Add(customInheritedType.GetJsonConverter(this.relationalMappings));
            }
        }
        
        public void Initialize(IEnumerable<DocumentMap> documentMaps, IEnumerable<CustomTypeDefinition> customTypeDefinitions = null)
        {
            if (documentMaps == null)
                throw new ArgumentException("DocumentMaps must be specified", nameof(documentMaps));

            if (customTypeDefinitions != null)
            {
                foreach (var customTypeDefinition in customTypeDefinitions)
                {
                    AddCustomTypeDefinition(customTypeDefinition);
                }
            }

            relationalMappings.Install(documentMaps);

            foreach (var documentMap in documentMaps)
            {
                if (documentMap is IDocumentHierarchyMap inheritedMap)
                {
                    AddCustomTypeDefinition(inheritedMap.CustomTypeDefinition);
                }

                // DocumentMap doesn't enforce IId on the document type, so the IdColumn can be null if the document
                // doesn't have an Id property 
                documentMap.IdColumn?.Initialize(this);

                foreach (var column in documentMap.IndexedColumns)
                {
                    column.Initialize(this);
                }
            }
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