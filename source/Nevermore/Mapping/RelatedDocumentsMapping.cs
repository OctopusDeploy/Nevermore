using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Nevermore.Advanced.PropertyHandlers;

namespace Nevermore.Mapping
{
    public class RelatedDocumentsMapping
    {
        public RelatedDocumentsMapping(PropertyInfo property, string tableName)
        {
            TableName = tableName;
            Handler = new PropertyHandler(property);
        }
        
        public string TableName { get; }
        public string IdColumnName => "Id";
        public string IdTableColumnName => "Table";
        public string RelatedDocumentIdColumnName => "RelatedDocumentId";
        public string RelatedDocumentTableColumnName => "RelatedDocumentTable";

        public IPropertyHandler Handler { get; }
    }
}