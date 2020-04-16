namespace Nevermore.Mapping
{
    public interface IColumnMappingBuilder
    {
        IColumnMappingBuilder Nullable();
        IColumnMappingBuilder MaxLength(int max);
        
        /// <summary>
        /// Nevermore will read values from the database and set them on this property, but will not include this proprty
        /// when performing updates or inserts. Useful for things like computed columns, rowversion, and so on.
        /// </summary>
        IColumnMappingBuilder LoadOnly();
        
        /// <summary>
        /// Nevermore will read this property and write the values to the database, but when reading, won't attempt to
        /// set this property (perhaps it has no public setter). Useful for things like calculated properties that return
        /// a value, but don't make sense to set when querying the database.
        /// </summary>
        IColumnMappingBuilder SaveOnly();
    }
}