using System;

namespace Nevermore.Mapping
{
    public interface IDatabaseValueConverter
    {
        /// <summary>
        /// If it can be converted, the <see cref="DatabaseValueConverter" /> will figure out how. Given a source
        /// object, tries its best to convert it to the target type.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="targetType">The type to convert the source object to.</param>
        /// <returns></returns>
        object ConvertFromDatabaseValue(object source, Type targetType);
    }
}