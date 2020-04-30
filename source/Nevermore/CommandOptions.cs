using System;

namespace Nevermore
{
    /// <summary>
    /// Common options for insert, update and delete commands.
    /// </summary>
    public abstract class CommandOptions
    {
        /// <summary>
        /// Gets or sets the command timeout. If null, uses the default command timeout.
        /// </summary>
        public TimeSpan? CommandTimeout { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the table that the query will use. If null, defaults to the table configured in the
        /// mapping for the type.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the schema of the table that the query will use. If null, defaults to the schema configured in the
        /// mapping for the type.
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Any hints to add when referencing the table.
        /// </summary>
        public string Hint { get; set; }
    }
}