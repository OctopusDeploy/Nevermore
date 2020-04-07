using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nevermore.Mapping
{
    public class RelatedDocumentsMapping
    {

        public RelatedDocumentsMapping(PropertyInfo property, string tableName)
        {
            TableName = tableName;
            ReaderWriter = PropertyReaderFactory.Create<IEnumerable<(string, Type)>>(property.DeclaringType, property.Name);
        }


        public string TableName { get; }
        public string IdColumnName => "Id";
        public string IdTableColumnName => "Table";
        public string RelatedDocumentIdColumnName => "RelatedDocumentId";
        public string RelatedDocumentTableColumnName => "RelatedDocumentTable";

        public IPropertyReaderWriter<IEnumerable<(string id, Type type)>> ReaderWriter { get; }
    }
}