namespace Nevermore.Mapping
{
    public enum JsonStorageFormat
    {
        /// <summary>
        /// The default. Use when you only have a [JSON] column.
        /// </summary>
        TextOnly,
        
        /// <summary>
        /// Use when you only have a [JSONBlob] column.
        /// </summary>
        CompressedOnly,
        
        /// <summary>
        /// Use when you have both [JSONBlob] and [JSON] columns, and want data in the [JSON] column to be migrated
        /// to the [JSONBlob] column.
        /// </summary>
        MixedPreferCompressed,
        
        /// <summary>
        /// Use when you have both [JSONBlob] and [JSON] columns, and want data in the [JSONBlob] column to be migrated
        /// to the [JSON] column.
        /// </summary>
        MixedPreferText
    }
}