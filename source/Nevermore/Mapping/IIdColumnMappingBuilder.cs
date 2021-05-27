namespace Nevermore.Mapping
{
    public interface IIdColumnMappingBuilder : IColumnMappingBuilder
    {
        /// <summary>
        /// Nevermore will treat this Id as an IDENTITY column, and will update the document with the Id assigned by the database after insert.
        /// </summary>
        /// <remarks>This will also reset the PropertyHandler</remarks>
        IIdColumnMappingBuilder Identity();
    }
}