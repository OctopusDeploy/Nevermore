#nullable enable
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Nevermore.Advanced.PropertyHandlers;

namespace Nevermore.Mapping
{
    public interface IChildTableMapping : IDocumentMap
    {
        Type DocumentType { get; }
        Type ChildDocumentType { get; }
        Type CollectionType { get; }
        Type ElementType { get; }

        IPropertyHandler PropertyHandler { get; }

        object ToChild(object parentDocument, object representationInParent);
        object FromChild(object childDocument);
    }

    public interface IForeignKeyColumnMappingBuilder : IColumnMappingBuilder
    {
        // Temp hack; The document map for a child table stores it's FK column in the `IdColumn` property.
        // If we were to do this properly we would create a proper `ForeignKeyColumn` property in `DocumentMap`,
        // and we may need to consider composite keys, not just a single column
        IdColumnMapping Build(IPrimaryKeyHandlerRegistry primaryKeyHandlerRegistry);
    }

    public class ForeignKeyColumnMappingBuilder : ColumnMapping, IForeignKeyColumnMappingBuilder
    {
        internal ForeignKeyColumnMappingBuilder(string columnName, Type type, IPropertyHandler handler, PropertyInfo property) : base(columnName, type, handler, property)
        {
        }

        // Bit of a hack, we should make a ForeignKeyColumnMapping instead, but going with minimal effort for POC
        public IdColumnMapping Build(IPrimaryKeyHandlerRegistry primaryKeyHandlerRegistry)
        {
            var primaryKeyHandler = primaryKeyHandlerRegistry.Resolve(Type)!;
            
            return new(ColumnName, Type, PropertyHandler, Property, false,
                primaryKeyHandler, ColumnDirection.FromDatabase, null);
        }
    }

    // A TCollection of TElement lives in the TParentDocument.
    // This gets mapped to multiple rows in a child table, using TChildDocument to serialize each row.
    public class ChildTableMapping<TParentDocument, TChildDocument, TElement, TCollection> :
        IChildTableMapping,
        IChildTableMappingBuilder<TParentDocument, TChildDocument, TElement, TCollection>,
        IDocumentMap
        where TCollection : IEnumerable<TElement>
        where TChildDocument : notnull
        where TElement : notnull
    {
        readonly Func<object, object, object> toChildObject;
        readonly Func<object, object> fromChildObject;

        IForeignKeyColumnMappingBuilder? foreignKeyColumn;
        readonly List<ColumnMapping> columns = new List<ColumnMapping>();

        public ChildTableMapping(
            Type childDocumentType,
            Type propertyType,
            IPropertyHandler propertyHandler,
            PropertyInfo? prop,
            Func<TParentDocument, TElement, TChildDocument> toChild,
            Func<TChildDocument, TElement> fromChild)
        {
            PropertyHandler = propertyHandler;
            toChildObject = (parentDocObj, objInParent) => toChild((TParentDocument)parentDocObj, (TElement)objInParent);
            fromChildObject = (childDocObj) => fromChild((TChildDocument)childDocObj);
            TableName = typeof(TChildDocument).Name;
        }

        public Type DocumentType => typeof(TParentDocument);
        public Type ChildDocumentType => typeof(TChildDocument);
        public Type CollectionType => typeof(TCollection);
        public Type ElementType => typeof(TElement);

        public IPropertyHandler PropertyHandler { get; }

        public DocumentMap Build(IPrimaryKeyHandlerRegistry primaryKeyHandlerRegistry)
        {
            var type = typeof(TChildDocument);

            if (foreignKeyColumn is null) throw new InvalidOperationException("Child tables must declare a ForeignKeyColumn which references back to the IdColumn on the parent table");

            var documentMap = new DocumentMap(type, TableName)
            {
                SchemaName = SchemaName,
                IdColumn = foreignKeyColumn.Build(primaryKeyHandlerRegistry),
                JsonStorageFormat = JsonStorageFormat,
                ExpectLargeDocuments = false,
                // RowVersionColumn not set; child documents should haven't DataVersion/RowVersion, that will be stored in the parent table
                // TypeResolutionColumn = Not implemented for now, build TypeResolution if/when we need it
            };
            documentMap.Columns.AddRange(columns);
            // documentMap.UniqueConstraints.AddRange(uniqueConstraints);
            // documentMap.RelatedDocumentsMappings.AddRange(relatedDocumentsMappings);
            // documentMap.ChildTables.AddRange(childTables); - In future we would likely need to add nested child tables e.g. DeploymentProcess

            return documentMap;
        }

        public object ToChild(object parentDocument, object representationInParent) => toChildObject(parentDocument, representationInParent);

        public object FromChild(object childDocument) => fromChildObject(childDocument);

        public string TableName { get; set; }

        protected string? SchemaName { get; set; }

        public IForeignKeyColumnMappingBuilder ForeignKeyColumn<TProperty>(Expression<Func<TChildDocument, TProperty>> getter)
        {
            return ForeignKeyColumn(null, getter, null);
        }

        protected IForeignKeyColumnMappingBuilder ForeignKeyColumn<TProperty>(string? columnName, Expression<Func<TChildDocument, TProperty>> getter, Action<TChildDocument, TProperty>? setter)
        {
            var prop = GetPropertyInfo(getter) ?? throw new ArgumentException("The expression for the Foreign Key column must be a property.");
            foreignKeyColumn = new ForeignKeyColumnMappingBuilder(columnName ?? prop.Name, typeof(TProperty), new PropertyHandler(prop), prop);
            return foreignKeyColumn;
        }

        public IColumnMappingBuilder Column<TProperty>(Expression<Func<TChildDocument, TProperty>> getter) => Column<TProperty>(null, getter, null);

        protected IColumnMappingBuilder Column<TProperty>(string? columnName, Expression<Func<TChildDocument, TProperty>>? getter, Action<TChildDocument, TProperty>? setter)
        {
            var property = GetPropertyInfo(getter ?? throw new ArgumentNullException(nameof(getter)));
            if (property != null && setter == null)
            {
                return Column(columnName ?? property.Name, typeof(TProperty), new PropertyHandler(property), property);
            }

            return Column(columnName ?? throw new ArgumentNullException(nameof(columnName)), typeof(TProperty), new DelegatePropertyHandler<TChildDocument, TProperty>(getter.Compile(), setter), property);
        }

        protected IColumnMappingBuilder Column(string columnName, Type propertyType, IPropertyHandler handler, PropertyInfo? prop = null)
        {
            var column = new ColumnMapping(columnName, propertyType, handler, prop);
            columns.Add(column);
            return column;
        }

        public JsonStorageFormat JsonStorageFormat { get; set; }

        static PropertyInfo? GetPropertyInfo<TSource, TProperty>(Expression<Func<TSource, TProperty>> propertyLambda) => DocumentMapHelper.GetPropertyInfo<TChildDocument, TSource, TProperty>(propertyLambda);
    }

    public interface IChildTableMappingBuilder<TParentDocument, TChildDocument, TElement, TCollection> where TCollection : IEnumerable<TElement>
    {
        IColumnMappingBuilder Column<TProperty>(Expression<Func<TChildDocument, TProperty>> getter);
        IForeignKeyColumnMappingBuilder ForeignKeyColumn<TProperty>(Expression<Func<TChildDocument, TProperty>> getter);
        JsonStorageFormat JsonStorageFormat { get; set; }
    }
}