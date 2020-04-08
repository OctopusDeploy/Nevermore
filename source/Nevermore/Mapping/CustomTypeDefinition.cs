using System;

namespace Nevermore.Mapping
{
    public abstract class CustomTypeDefinition : CustomTypeDefinitionBase
    {
        /// <summary>
        /// Get the value to write when the type is used in the JSON column.
        /// </summary>
        /// <param name="instance">The object instance being written</param>
        /// <returns>The converted object value to write, or null to let the JsonSerializer handle the serialization.</returns>
        public virtual object ConvertToJsonColumnValue(object instance)
        {
            return null;
        }

        /// <summary>
        /// Get the value to write when the type is used in its own column (i.e. something in IndexedColumns in the DocumentMap).
        /// </summary>
        /// <param name="instance">The object instance being written</param>
        /// <returns>The converted object to write, which should be consistent with the DbType that was specified for this custom type.</returns>
        public virtual object ConvertToIndexedColumnDbValue(object instance)
        {
            return instance;
        }

        /// <summary>
        /// Get an object instance from the database value that was stored in the JSON column.
        /// </summary>
        /// <param name="value">The database value that was read.</param>
        /// <param name="targetType">The target type where the value is being written to.</param>
        /// <returns>The converted object, or null for the JsonSerializer to handler the read.</returns>
        public virtual object ConvertFromJsonDbValue(object value, Type targetType)
        {
            return null;
        }

        /// <summary>
        /// Get an object instance from the database value that was stored in its own column.
        /// </summary>
        /// <param name="value">The database value that was read.</param>
        /// <param name="targetType">The target type where the value is being written to.</param>
        /// <returns>The converted object.</returns>
        public virtual object ConvertFromIndexedColumnDbValue(object value, Type targetType)
        {
            return value;
        }
    }
}