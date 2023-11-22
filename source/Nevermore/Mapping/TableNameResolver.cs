using System;

namespace Nevermore.Mapping
{
    internal class TableNameResolver : ITableNameResolver
    {
        readonly IDocumentMapRegistry mappings;

        public TableNameResolver(IDocumentMapRegistry mappings)
        {
            this.mappings = mappings;
        }

        public string GetTableNameFor(Type documentType) => mappings.Resolve(documentType).TableName;
    }
}