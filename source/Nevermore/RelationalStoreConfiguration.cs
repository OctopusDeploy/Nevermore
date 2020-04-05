using System;
using System.Collections.Generic;
using Nevermore.Contracts;
using Nevermore.Mapping;
using Nevermore.Serialization;
using Newtonsoft.Json;

namespace Nevermore
{
    public class RelationalStoreConfiguration
    {
        public RelationalStoreConfiguration(IEnumerable<ICustomTypeDefinition> customTypeDefinitions)
        {
            AmazingConverter = new AmazingConverter(this);
            
            RelationalMappings = new RelationalMappings();

            if (customTypeDefinitions != null)
            {
                foreach (var customTypeDefinition in customTypeDefinitions)
                {
                    AddCustomTypeDefinition(customTypeDefinition);
                }
            }
        }

        public RelationalMappings RelationalMappings { get; }
        public JsonSerializerSettings JsonSettings { get; internal set; } = new JsonSerializerSettings();

        public Dictionary<Type, ICustomTypeDefinition> CustomTypeDefinitions { get; } = new Dictionary<Type, ICustomTypeDefinition>();

        public IAmazingConverter AmazingConverter { get; }

        void AddCustomTypeDefinition(ICustomTypeDefinition customTypeDefinition)
        {
            var jsonConverter = new CustomTypeConverter(customTypeDefinition);
            JsonSettings.Converters.Add(jsonConverter);
            
            CustomTypeDefinitions.Add(customTypeDefinition.ModelType, customTypeDefinition);
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
    }
}