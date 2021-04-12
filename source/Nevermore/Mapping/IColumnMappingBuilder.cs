using Nevermore.Advanced.TypeHandlers;

namespace Nevermore.Mapping
{
    public interface IColumnMappingBuilder
    {
        IColumnMappingBuilder MaxLength(int max);

        /// <summary>
        /// Nevermore will read values from the database and set them on this property, but will not include this property
        /// when performing updates or inserts. Useful for things like computed columns, rowversion, and so on.
        /// </summary>
        IColumnMappingBuilder LoadOnly();

        /// <summary>
        /// Nevermore will read this property and write the values to the database, but when reading, won't attempt to
        /// set this property (perhaps it has no public setter). Useful for things like calculated properties that return
        /// a value, but don't make sense to set when querying the database.
        /// </summary>
        IColumnMappingBuilder SaveOnly();

        /// <summary>
        /// Nevermore will read values from the database and set them on this property. This property will be used
        /// when performing updates to make sure the data in the database hasn't changed.
        /// </summary>
        IColumnMappingBuilder RowVersion();

        /// <summary>
        /// Nevermore will build an expression to read and write the property automatically. However, you can override
        /// this behavior with your own property handler. Keep in mind that if you want to control how a type is mapped
        /// from the database to a .NET object, you might want to use a <see cref="ITypeHandler"/> instead.
        /// </summary>
        /// <param name="propertyHandler">The property handler to use.</param>
        /// <returns></returns>
        IColumnMappingBuilder CustomPropertyHandler(IPropertyHandler propertyHandler);
    }
}