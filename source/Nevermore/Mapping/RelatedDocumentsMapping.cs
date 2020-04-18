using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Nevermore.Mapping
{
    public class RelatedDocumentsMapping
    {
        public RelatedDocumentsMapping(PropertyInfo property, string tableName)
        {
            TableName = tableName;
            Handler = PropertyReaderFactory.Create<IEnumerable<(string, Type)>>(property.DeclaringType, property.Name);
        }
        
        public string TableName { get; }
        public string IdColumnName => "Id";
        public string IdTableColumnName => "Table";
        public string RelatedDocumentIdColumnName => "RelatedDocumentId";
        public string RelatedDocumentTableColumnName => "RelatedDocumentTable";

        public IPropertyHandler Handler { get; }
    }
}