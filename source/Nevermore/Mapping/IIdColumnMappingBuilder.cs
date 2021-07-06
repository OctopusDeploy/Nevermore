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
        /// Builds the IdColumnMapping.
        /// </summary>
        /// <param name="primaryKeyHandlerRegistry">The primary key handler registry, which must be populated with the required handler prior to the DocumentMaps being registered.</param>
        /// <returns>The built column mapping.</returns>
        IdColumnMapping Build(IPrimaryKeyHandlerRegistry primaryKeyHandlerRegistry);
    }
}