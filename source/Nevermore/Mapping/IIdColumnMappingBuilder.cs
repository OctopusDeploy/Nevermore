using System;

namespace Nevermore.Mapping
{
    public interface IIdColumnMappingBuilder : IColumnMappingBuilder
    {
        /// <summary>
        /// Nevermore will treat this Id as an IDENTITY column, and will update the document with the Id assigned by the database after insert.
        /// </summary>
        /// <remarks>This will also reset the PropertyHandler</remarks>
        IIdColumnMappingBuilder Identity();

        /// <summary>
        /// Explicitly set a primary key handler.
        /// </summary>
        /// <param name="primaryKeyHandler">The primary key handler.</param>
        /// <returns></returns>
        IIdColumnMappingBuilder KeyHandler(IPrimaryKeyHandler primaryKeyHandler);

        /// <summary>
        /// Set a function that when given the TableName will return key prefix string.
        /// </summary>
        /// <param name="idPrefix">The function to call back to get the prefix.</param>
        IIdColumnMappingBuilder Prefix(Func<string, string> idPrefix);

        /// <summary>
        /// Set a function that format a key value, given a prefix and a key number.
        /// </summary>
        /// <param name="format">The function to call back to format the id.</param>
        IIdColumnMappingBuilder Format(Func<(string idPrefix, int key), string> format);

        /// <summary>
        /// Builds the IdColumnMapping.
        /// </summary>
        /// <param name="primaryKeyHandlerRegistry">The primary key handler registry, which must be populated with the required handler prior to the DocumentMaps being registered.</param>
        /// <returns>The built column mapping.</returns>
        IdColumnMapping Build(IPrimaryKeyHandlerRegistry primaryKeyHandlerRegistry);
    }
}