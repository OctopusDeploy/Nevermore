using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Nevermore.Mapping
{
    /// <summary>
    /// The one and only <see cref="DatabaseValueConverter" />. Can convert from absolutely anything to absolutely
    /// anything.
    /// </summary>
    class DatabaseValueConverter : IDatabaseValueConverter
    {
        readonly RelationalStoreConfiguration relationalStoreConfiguration;
    
        public DatabaseValueConverter(RelationalStoreConfiguration relationalStoreConfiguration)
        {
            this.relationalStoreConfiguration = relationalStoreConfiguration;
        }
    
        /// <summary>
        /// If it can be converted, the <see cref="DatabaseValueConverter" /> will figure out how. Given a source
        /// object, tries its best to convert it to the target type.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="targetType">The type to convert the source object to.</param>
        /// <returns></returns>
        public object ConvertFromDatabaseValue(object source, Type targetType)
        {
            // Defer to the AmazingConverter, if that come up with a result then try the custom type definitions
            var convertedValue = AmazingConverter.Convert(source, targetType);
            if (convertedValue != source)
                return convertedValue;
    
            if (relationalStoreConfiguration != null && relationalStoreConfiguration.TryGetCustomTypeDefinitionForType(targetType, out var customTypeDefinition))
            {
                return customTypeDefinition.ConvertFromIndexedColumnDbValue(source, targetType);
            }
    
            // Hope and pray
            return source;
        }
    }
}