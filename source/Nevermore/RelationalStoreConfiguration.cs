using System;
using System.Collections.Generic;
using System.Linq;
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

        public RelationalStoreConfiguration(IEnumerable<ICustomTypeDefinition> customTypeDefinitions)
        {
            AmazingConverter = new AmazingConverter(this);
            
            relationalMappings = new RelationalMappings();
            
            jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new RelationalJsonContractResolver(relationalMappings),
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
            };

            if (customTypeDefinitions != null)
            {
                foreach (var customTypeDefinition in customTypeDefinitions)
                {
                    AddCustomTypeDefinition(customTypeDefinition);
                }
            }
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

        void AddCustomTypeDefinition(ICustomTypeDefinition customTypeDefinition)
        {
            if (customTypeDefinition is CustomSingleTypeDefinition customType)
            {
                var jsonConverter = new CustomTypeConverter(customType);
                jsonSettings.Converters.Add(jsonConverter);

                CustomSingleTypeDefinitions.Add(customType.ModelType, customType);
            }
            if (customTypeDefinition is ICustomInheritedTypeDefinition customInheritedType)
            {
                jsonSettings.Converters.Add(customInheritedType.GetJsonConverter(this.relationalMappings));
            }
        }
        
        public void AddCustomTypeDefinitions(IEnumerable<ICustomTypeDefinition> customTypeDefinitions)
        {
            if (customTypeDefinitions == null)
                return;
            
            foreach (var customTypeDefinition in customTypeDefinitions)
            {
                AddCustomTypeDefinition(customTypeDefinition);
            }
        }

        public void AddDocumentMaps(IEnumerable<DocumentMap> documentMaps)
        {
            relationalMappings.Install(documentMaps);

            foreach (var inheritedMap in documentMaps.OfType<IDocumentHierarchyMap>())
            {
                AddCustomTypeDefinition(inheritedMap.CustomTypeDefinition);
            }
        }

        public string SerializeObject(object value, Type type)
        {
            return JsonConvert.SerializeObject(value, type, jsonSettings);
        }
        public object DeserializeObject(string value, Type type)
        {
            return JsonConvert.DeserializeObject(value, type, jsonSettings);
        }
    }
}