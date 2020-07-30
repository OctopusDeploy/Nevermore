using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Nevermore.Advanced.PropertyHandlers;

namespace Nevermore.Mapping
{
    public class RelatedDocumentsMapping
    {
        public RelatedDocumentsMapping(PropertyInfo property, string tableName, string schemaName)
        {
            TableName = tableName;
            SchemaName = schemaName;
            Handler = new PropertyRelatedDocumentsRetriever(new PropertyHandler(property));
        }

        public RelatedDocumentsMapping(IRelatedDocumentsRetriever relatedDocumentsRetriever, string tableName, string schemaName)
        {
            TableName = tableName;
            SchemaName = schemaName;
            Handler = relatedDocumentsRetriever;
        }

        public string TableName { get; }
        public string SchemaName { get; }
        public string IdColumnName => "Id";
        public string IdTableColumnName => "Table";
        public string RelatedDocumentIdColumnName => "RelatedDocumentId";
        public string RelatedDocumentTableColumnName => "RelatedDocumentTable";

        public IRelatedDocumentsRetriever Handler { get; }
    }

    public interface IRelatedDocumentsRetriever
    {
        IEnumerable<(string id, Type type)> Read(object target);
    }

    class PropertyRelatedDocumentsRetriever : IRelatedDocumentsRetriever
    {
        readonly IPropertyHandler propertyHandler;

        public PropertyRelatedDocumentsRetriever(IPropertyHandler propertyHandler)
        {
            this.propertyHandler = propertyHandler;
        }

        public IEnumerable<(string id, Type type)> Read(object target)
        {
            return propertyHandler.Read(target) as IEnumerable<(string id, Type type)> ?? new (string id, Type type)[0];
        }
    }
}