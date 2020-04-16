using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Nevermore.Mapping
{
    
    public abstract class DocumentMap<TDocument> : DocumentMap
    {
        protected DocumentMap()
        {
            InitializeDefault(typeof (TDocument));
        }

        protected IColumnMappingBuilder Column<T>(string columnName, IPropertyReaderWriter readerWriter)
        {
            var column = new ColumnMapping(columnName, typeof(T), readerWriter);
            IndexedColumns.Add(column);
            return column;
        }
        
        protected IColumnMappingBuilder Column<T>(Expression<Func<TDocument, T>> property, string columnName = null)
        {
            var column = new ColumnMapping(columnName, GetPropertyInfo(property));
            IndexedColumns.Add(column);
            return column;
        }

        protected RelatedDocumentsMapping RelatedDocuments(Expression<Func<TDocument, IEnumerable<(string, Type)>>> property, string tableName = DefaultRelatedDocumentTableName)
        {
            var mapping = new RelatedDocumentsMapping(GetPropertyInfo(property), tableName);
            RelatedDocumentsMappings.Add(mapping);
            return mapping;
        }

        static PropertyInfo GetPropertyInfo<TSource, TProperty>(Expression<Func<TSource, TProperty>> propertyLambda)
        {
            var member = propertyLambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    propertyLambda));

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a field, not a property.",
                    propertyLambda));

            return propInfo;
        }

        protected UniqueRule Unique(string constraintName, string columnName, string errorMessage)
        {
            var unique = new UniqueRule(constraintName, columnName) {Message = errorMessage};
            UniqueConstraints.Add(unique);
            return unique;
        }

        protected UniqueRule Unique(string constraintName, string[] columnNames, string errorMessage)
        {
            var unique = new UniqueRule(constraintName, columnNames) {Message = errorMessage};
            UniqueConstraints.Add(unique);
            return unique;
        }
    }

    public abstract class DocumentMap
    {
        public const string DefaultRelatedDocumentTableName = "RelatedDocument";
        
        protected DocumentMap()
        {
            IndexedColumns = new List<ColumnMapping>();
            UniqueConstraints = new List<UniqueRule>();
            RelatedDocumentsMappings = new List<RelatedDocumentsMapping>();
        }

        public string TableName { get; protected set; }
        public string IdPrefix { get; protected set; }
        public Func<int, string> IdFormat { get; protected set; }

        public Type Type { get; protected set; }
        public ColumnMapping IdColumn { get; private set; }
        
        public JsonStorageFormat JsonStorageFormat { get; set; }

        /// <summary>
        /// Columns containing data that could be indexed (but are not necessarily indexed)
        /// </summary>
        public List<ColumnMapping> IndexedColumns { get; private set; }
        public List<UniqueRule> UniqueConstraints { get; private set; }
        public List<RelatedDocumentsMapping> RelatedDocumentsMappings { get; private set; }
        
        // TODO: Obsolete
        public string SingletonId { get; protected set; }

        protected void InitializeDefault(Type type)
        {
            Type = type;
            TableName = type.Name;
            IdPrefix = TableName + "s";
            IdFormat = key => $"{IdPrefix}-{key}";

            var properties = type.GetTypeInfo().GetProperties();

            JsonStorageFormat = JsonStorageFormat.TextOnly;

            foreach (var property in properties)
            {
                if (string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    IdColumn = new ColumnMapping("Id", property);
                }
            }
        }
    }
}