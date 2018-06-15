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
            ReaderWriter = PropertyReaderFactory.Create<IEnumerable<string>>(property.DeclaringType, property.Name);
        }


        public string TableName { get; }
        public string IdColumnName => "Id";
        public string RelatedDocumentIdColumnName => "RelatedDocumentId";

        public IPropertyReaderWriter<IEnumerable<string>> ReaderWriter { get; }
    }
}