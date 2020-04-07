using System;

namespace Nevermore.Mapping
{
    public abstract class CustomTypeDefinition : CustomTypeDefinitionBase
    {
        /// <summary>
        /// Get the value to write to the database. Only override this in order to take over complete control of the
        /// serialization for both columns and JSON.
        /// </summary>
        /// <param name="instance">The object instance being written</param>
        /// <param name="isForJsonSerialization">True for serialization into the JSON column.</param>
        /// <returns>The object value to write, or null to let the JsonSerializer handle the serialization.</returns>
        public virtual object ToDbValue(object instance, bool isForJsonSerialization)
        {
            if (isForJsonSerialization)
                return null;
            return ConvertToColumnDbValue(instance);
        }

        public virtual object ConvertToColumnDbValue(object instance)
        {
            return instance;
        }

        /// <summary>
        /// Get an object instance from the database value. Only override this in order to take over complete control of the
        /// serialization for both columns and JSON.
        /// </summary>
        /// <param name="value">The database value that was read.</param>
        /// <param name="targetType">The target type where the value is being written to.</param>
        /// <param name="isForJsonSerialization">True for serialization into the JSON column.</param>
        /// <returns>The resulting object value, or null for the JsonSerializer to handler the read.</returns>
        public virtual object FromDbValue(object value, Type targetType, bool isForJsonSerialization)
        {
            if (isForJsonSerialization)
                return null;
            return ConvertFromColumnDbValue((string)value, targetType);
        }

        public virtual object ConvertFromColumnDbValue(object value, Type targetType)
        {
            return value;
        }
    }
}