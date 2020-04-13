namespace Nevermore
{
    /// <summary>
    /// Options for Insert commands.
    /// </summary>
    public class InsertOptions : CommandOptions
    {
        /// <summary>
        /// Gets or sets a specific ID to assign to the document being inserted. If null (the default) it will assign
        /// an ID automatically using the <see cref="T:IKeyAllocator"/>.
        /// </summary>
        public string CustomAssignedId { get; set; }

        /// <summary>
        /// Gets or sets whether to include the [Id] and [Json] columns (defaults to true). If false, these columns
        /// will be omitted. Useful when inserting into another table.
        /// </summary>
        public bool IncludeDefaultModelColumns { get; set; } = true;
        
        public static readonly InsertOptions Default = new InsertOptions();     // All values null by default
    }
}