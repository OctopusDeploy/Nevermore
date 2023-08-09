#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Nevermore.Advanced.InstanceTypeResolvers;
using Nevermore.Advanced.PropertyHandlers;

namespace Nevermore.Mapping
{
    public abstract class DocumentMap<TDocument> : IDocumentMap
    {
        IIdColumnMappingBuilder? idColumn;
        ColumnMapping? rowVersionColumn;
        ColumnMapping? typeResolutionColumn;
        readonly List<ColumnMapping> columns = new List<ColumnMapping>();
        readonly List<IChildTableMapping> childTables = new List<IChildTableMapping>();
        readonly List<RelatedDocumentsMapping> relatedDocumentsMappings = new List<RelatedDocumentsMapping>();
        readonly List<UniqueRule> uniqueConstraints = new List<UniqueRule>();

        protected DocumentMap()
        {
            TableName = typeof(TDocument).Name;
        }

        /// <summary>
        /// Gets or sets the name of the schema containing the table that this document will be stored in.
        /// </summary>
        protected string? SchemaName { get; set; }

        /// <summary>
        /// Gets or sets the name of the table that this document will be stored in.
        /// </summary>
        protected string TableName { get; set; }

        /// <summary>
        /// Tells Nevermore whether to expect large documents or not. Defaults to false, since most tables tend to only
        /// have small documents. However, this property is self-tuning: if Nevermore reads or writes a document
        /// larger than 1K, it will set this to true.
        /// </summary>
        protected bool ExpectLargeDocuments { get; set; }

        /// <summary>
        /// Gets or sets the JSON storage mode. See https://github.com/OctopusDeploy/Nevermore/wiki/Compression for details.
        /// </summary>
        protected JsonStorageFormat JsonStorageFormat { get; set; }

        /// <summary>
        /// Configures the ID of the document.
        /// </summary>
        /// <returns>A builder to further configure the ID.</returns>
        protected IIdColumnMappingBuilder Id()
        {
            idColumn = GetDefaultIdColumn();
            if (idColumn is null)
                throw new InvalidOperationException($"Unable to determine default Id property for {typeof(TDocument).Name}.");
            return idColumn;
        }

        /// <summary>
        /// Configures the ID of the document.
        /// </summary>
        /// <param name="property">An expression that accesses the property. E.g., <code>c => c.FirstName</code></param>
        /// <typeparam name="TProperty">The property type of the Id column.</typeparam>
        /// <returns>A builder to further configure the ID.</returns>
        protected IIdColumnMappingBuilder Id<TProperty>(Expression<Func<TDocument, TProperty>> property)
        {
            return Id(null, property);
        }

        /// <summary>
        /// Configures the ID of the document.
        /// </summary>
        /// <param name="columnName">The name of the column that the ID is stored in.</param>
        /// <param name="property">An expression that accesses the property. E.g., <code>c => c.FirstName</code></param>
        /// <typeparam name="TProperty">The property type of the Id column.</typeparam>
        /// <returns>A builder to further configure the ID.</returns>
        protected IIdColumnMappingBuilder Id<TProperty>(string? columnName, Expression<Func<TDocument, TProperty>> property)
        {
            var prop = GetPropertyInfo(property)
                ?? throw new Exception("The expression for the Id column must be a property.");
            idColumn = new IdColumnMappingBuilder(columnName ?? prop.Name, typeof(TProperty), new PropertyHandler(prop), prop);
            return idColumn;
        }

        /// <summary>
        /// Defines a column that will be used to resolve the type of object to create. Nevermore will then locate an
        /// <see cref="IInstanceTypeResolver"/> that can resolve concrete instances of that type based on the value
        /// in this column.
        /// </summary>
        /// <param name="property">An expression that accesses the property. E.g., <code>c => c.FirstName</code></param>
        /// <typeparam name="TProperty">The property type of the column.</typeparam>
        /// <returns>A builder to further configure the column mapping.</returns>
        protected IColumnMappingBuilder TypeResolutionColumn<TProperty>(Expression<Func<TDocument, TProperty>> property)
        {
            return TypeResolutionColumn(null, property);
        }

        /// <summary>
        /// Defines a column that will be used to resolve the type of object to create. Nevermore will then locate an
        /// <see cref="IInstanceTypeResolver"/> that can resolve concrete instances of that type based on the value
        /// in this column.
        /// </summary>
        /// <param name="columnName">The name of the column that the property will be stored in.</param>
        /// <param name="property">An expression that accesses the property. E.g., <code>c => c.FirstName</code></param>
        /// <typeparam name="TProperty">The property type of the column.</typeparam>
        /// <returns>A builder to further configure the column mapping.</returns>
        protected IColumnMappingBuilder TypeResolutionColumn<TProperty>(string? columnName, Expression<Func<TDocument, TProperty>> property)
        {
            var prop = GetPropertyInfo(property)
                       ?? throw new Exception("The expression for the Type Resolution column must be a property.");
            typeResolutionColumn = new ColumnMapping(columnName ?? prop.Name, typeof(TProperty), new PropertyHandler(prop), prop);
            columns.Add(typeResolutionColumn);
            return typeResolutionColumn;
        }

        /// <summary>
        /// Defines a column. The column name will be the name of the property.
        /// </summary>
        /// <param name="getter">An expression that accesses the property. E.g., <code>c => c.FirstName</code></param>
        /// <typeparam name="TProperty">The property type of the column.</typeparam>
        /// <returns>A builder to further configure the column mapping.</returns>
        protected IColumnMappingBuilder Column<TProperty>(Expression<Func<TDocument, TProperty>> getter)
        {
            return Column(null, getter, null);
        }

        protected void RowVersion<TProperty>(Expression<Func<TDocument, TProperty>> getter)
        {
            rowVersionColumn = (ColumnMapping)Column(null, getter, null).LoadOnly();
        }

        /// <summary>
        /// Defines a column. The column name will be the name of the property.
        /// </summary>
        /// <param name="columnName">The name of the column that the property will be stored in.</param>
        /// <param name="getter">An expression that accesses the property. E.g., <code>c => c.FirstName</code></param>
        /// <typeparam name="TProperty">The property type of the column.</typeparam>
        /// <returns>A builder to further configure the column mapping.</returns>
        protected IColumnMappingBuilder Column<TProperty>(string columnName, Expression<Func<TDocument, TProperty>> getter)
        {
            return Column(columnName, getter, null);
        }

        /// <summary>
        /// Defines a column. The column name will be the name of the property.
        /// </summary>
        /// <param name="columnName">The name of the column that the property will be stored in.</param>
        /// <param name="getter">An expression that accesses the property. E.g., <code>c => c.FirstName</code></param>
        /// <param name="setter">A func called when reading data from the database and setting it on the object.</param>
        /// <typeparam name="TProperty">The property type of the column.</typeparam>
        /// <returns>A builder to further configure the column mapping.</returns>
        protected IColumnMappingBuilder Column<TProperty>(string? columnName, Expression<Func<TDocument, TProperty>>? getter, Action<TDocument, TProperty>? setter)
        {
            var property = GetPropertyInfo(getter ?? throw new ArgumentNullException(nameof(getter)));
            if (property != null && setter == null)
            {
                return Column(columnName ?? property.Name, typeof(TProperty), new PropertyHandler(property), property);
            }

            return Column(columnName ?? throw new ArgumentNullException(nameof(columnName)), typeof(TProperty), new DelegatePropertyHandler<TDocument, TProperty>(getter.Compile(), setter), property);
        }

        /// <summary>
        /// Defines a column. The column name will be the name of the property.
        /// </summary>
        /// <param name="columnName">The name of the column that the property will be stored in.</param>
        /// <param name="propertyType">The type of the property being read or written. This helps Nevermore to work out how to read it from the database, and which type handlers to call.</param>
        /// <param name="handler">A custom property handler.</param>
        /// <param name="prop">If handler uses a property, the property, so it can be excluded from JSON. Can be null.</param>
        /// <returns>A builder to further configure the column mapping.</returns>
        protected IColumnMappingBuilder Column(string columnName, Type propertyType, IPropertyHandler handler, PropertyInfo? prop = null)
        {
            var column = new ColumnMapping(columnName, propertyType, handler, prop);
            columns.Add(column);
            return column;
        }

        // If our Document has a collection, and that collection would be stored in a "child" table, express it with DependentCollection
        protected IChildTableMappingBuilder<TDocument, TChildDocument, TElement, TCollection> DependentCollection<TChildDocument, TElement, TCollection>(
            Expression<Func<TDocument, TCollection>> getter,
            Action<IChildTableMappingBuilder<TDocument, TChildDocument, TElement, TCollection>> mappingBuilder,
            Func<TDocument, TElement, TChildDocument> toChild,
            Func<TChildDocument, TElement> fromChild)
            where TCollection : IEnumerable<TElement> where TChildDocument : notnull where TElement : notnull
        {
            var property = GetPropertyInfo(getter) ?? throw new ArgumentException(nameof(getter));
            // TODO OE: Support for setter
            
            var childMapping = new ChildTableMapping<TDocument, TChildDocument, TElement, TCollection>(
                typeof(TChildDocument),
                property.PropertyType, 
                new PropertyHandler(property), 
                property, 
                toChild,
                fromChild);
            mappingBuilder(childMapping);
            childTables.Add(childMapping);

            return childMapping;
        }

        protected RelatedDocumentsMapping RelatedDocuments(Expression<Func<TDocument, IEnumerable<(string, Type)>>> property, string tableName = DocumentMap.RelatedDocumentTableName, string? schemaName = null)
        {
            var mapping = new RelatedDocumentsMapping(GetPropertyInfo(property), tableName, schemaName);
            relatedDocumentsMappings.Add(mapping);
            return mapping;
        }

        /// <summary>
        /// Defines a unique constraint. Nevermore will provide friendly error details if the unique constraint is violated.
        /// </summary>
        /// <param name="constraintName">The name of the constraint in SQL (or at least a partial match).</param>
        /// <param name="columnName">The name of the column that the constraint relates to.</param>
        /// <param name="errorMessage">An error message to put in the exception when the unique constraint is violated.</param>
        /// <returns>A builder to further configure the unique constraint.</returns>
        protected UniqueRule Unique(string constraintName, string columnName, string errorMessage)
        {
            var unique = new UniqueRule(constraintName, columnName) {Message = errorMessage};
            uniqueConstraints.Add(unique);
            return unique;
        }

        /// <summary>
        /// Defines a unique constraint. Nevermore will provide friendly error details if the unique constraint is violated.
        /// </summary>
        /// <param name="constraintName">The name of the constraint in SQL (or at least a partial match).</param>
        /// <param name="columnNames">The name of the columns that the constraint relates to.</param>
        /// <param name="errorMessage">An error message to put in the exception when the unique constraint is violated.</param>
        /// <returns>A builder to further configure the unique constraint.</returns>
        protected UniqueRule Unique(string constraintName, string[] columnNames, string errorMessage)
        {
            var unique = new UniqueRule(constraintName, columnNames) {Message = errorMessage};
            uniqueConstraints.Add(unique);
            return unique;
        }

        static PropertyInfo? GetPropertyInfo<TSource, TProperty>(Expression<Func<TSource, TProperty>> propertyLambda) => DocumentMapHelper.GetPropertyInfo<TDocument, TSource, TProperty>(propertyLambda);

        static IIdColumnMappingBuilder? GetDefaultIdColumn()
        {
            var properties = typeof(TDocument).GetProperties();
            foreach (var property in properties)
            {
                if (string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    return new IdColumnMappingBuilder("Id", property.PropertyType, new PropertyHandler(property), property);
                }
            }

            return null;
        }

        DocumentMap IDocumentMap.Build(IPrimaryKeyHandlerRegistry primaryKeyHandlerRegistry)
        {
            if (idColumn is null)
                idColumn = GetDefaultIdColumn();

            var builtIdColumn = idColumn?.Build(primaryKeyHandlerRegistry);

            var type = typeof(TDocument);

            var documentMap = new DocumentMap(type, TableName)
            {
                SchemaName = SchemaName,
                IdColumn = builtIdColumn,
                JsonStorageFormat = JsonStorageFormat,
                ExpectLargeDocuments = ExpectLargeDocuments,
                RowVersionColumn = rowVersionColumn,
                TypeResolutionColumn = typeResolutionColumn
            };
            documentMap.Columns.AddRange(columns);
            documentMap.UniqueConstraints.AddRange(uniqueConstraints);
            documentMap.RelatedDocumentsMappings.AddRange(relatedDocumentsMappings);
            documentMap.ChildTables.AddRange(childTables);

            return documentMap;
        }
    }

    static class DocumentMapHelper
    {
        internal static PropertyInfo? GetPropertyInfo<TDocument, TSource, TProperty>(Expression<Func<TSource, TProperty>> propertyLambda)
        {
            var member = propertyLambda.Body as MemberExpression;
            if (member == null)
                return null;

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                return null;

            var type = typeof(TDocument);
            if (propInfo.DeclaringType != null && (type == propInfo.DeclaringType || propInfo.DeclaringType.IsAssignableFrom(type)))
            {
                return propInfo;
            }

            return null;
        }
    }

    public class DocumentMap
    {
        public const string RelatedDocumentTableName = "RelatedDocument";

        public DocumentMap(Type type, string tableName)
        {
            Type = type;
            TableName = tableName;
            Columns = new List<ColumnMapping>();
            UniqueConstraints = new List<UniqueRule>();
            RelatedDocumentsMappings = new List<RelatedDocumentsMapping>();
            ChildTables = new List<IChildTableMapping>();
        }

        public Type Type { get; }
        public IdColumnMapping? IdColumn { get; set; }
        public ColumnMapping? RowVersionColumn { get; set; }
        public ColumnMapping? TypeResolutionColumn { get; set; }
        public JsonStorageFormat JsonStorageFormat { get; set; }
        public string TableName { get; }

        public bool ExpectLargeDocuments { get; set; }

        /// <summary>
        /// Columns containing data that could be indexed (but are not necessarily indexed)
        /// </summary>
        public List<ColumnMapping> Columns { get; }
        public List<UniqueRule> UniqueConstraints { get; }
        public List<RelatedDocumentsMapping> RelatedDocumentsMappings { get; }
        public List<IChildTableMapping> ChildTables { get; }
        public string? SchemaName { get; set; }

        public bool IsRowVersioningEnabled => RowVersionColumn != null;

        public bool IsIdentityId => IdColumn?.Direction == ColumnDirection.FromDatabase;

        public bool HasModificationOutputs =>IsRowVersioningEnabled || IsIdentityId;

        public void Validate()
        {
            if (IdColumn == null)
                throw new InvalidOperationException($"There is no Id property on the document type {Type.FullName}");

            if (TypeResolutionColumn != null && JsonStorageFormat == JsonStorageFormat.NoJson)
                throw new InvalidOperationException($"The document map for type {Type.FullName} has a TypeColumn, but also uses the NoJson storage mode, which is not allowed.");

            try
            {
                foreach (var column in Columns)
                    column.Validate();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Validation error on document map for type {Type.FullName}: " + ex.Message, ex);
            }
        }

        public object? GetId(object? document)
        {
            if (document == null || IdColumn == null)
                return null;

            var readerWriter = IdColumn.PropertyHandler;
            return readerWriter.Read(document);
        }

        public bool HasJsonColumn() => new[] { JsonStorageFormat.TextOnly, JsonStorageFormat.MixedPreferCompressed, JsonStorageFormat.MixedPreferText }.Contains(JsonStorageFormat);
        public bool HasJsonBlobColumn() => new[] { JsonStorageFormat.CompressedOnly, JsonStorageFormat.MixedPreferCompressed, JsonStorageFormat.MixedPreferText }.Contains(JsonStorageFormat);
    }
}