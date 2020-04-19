using System;
using System.Reflection;
using Nevermore.Advanced.PropertyHandlers;

namespace Nevermore.Mapping
{
    public class ColumnMapping : IColumnMappingBuilder
    {
        const int MaxStringLengthByDefault = 200;
        const int DefaultPrimaryKeyIdLength = 50;
        const int DefaultMaxForeignKeyIdLength = 50;
        const int DefaultMaxEnumLength = 50;
        ColumnDirection direction;
        int? maxLength;
        bool isNullable;

        internal ColumnMapping(string columnName, Type type, IPropertyHandler handler)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            PropertyHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        }
        
        internal ColumnMapping(string columnName, PropertyInfo property)
        {
            Property = property;
            Type = property.PropertyType;
            ColumnName = columnName ?? property.Name;
            PropertyHandler = new PropertyHandler(property);

            if (property.PropertyType.GetTypeInfo().IsGenericType && typeof(Nullable<>).GetTypeInfo().IsAssignableFrom(property.PropertyType.GetGenericTypeDefinition()))
            {
                isNullable = true;
            }
            
            if (property.Name == "Id")
            {
                maxLength = DefaultPrimaryKeyIdLength;
            }
            else if (property.Name.EndsWith("Id")) // Foreign keys
            {
                maxLength = DefaultMaxForeignKeyIdLength;
            }

            if (property.PropertyType.GetTypeInfo().IsEnum)
            {
                maxLength = DefaultMaxEnumLength;
            }
        }

        public string ColumnName { get; }
        public Type Type { get; }
        public IPropertyHandler PropertyHandler { get; private set; }
        public PropertyInfo Property { get; }

        public bool IsNullable => isNullable;
        public int MaxLength => maxLength ?? MaxStringLengthByDefault;
        public ColumnDirection Direction => direction;

        IColumnMappingBuilder IColumnMappingBuilder.Nullable()
        {
            isNullable = true;
            return this;
        }

        IColumnMappingBuilder IColumnMappingBuilder.MaxLength(int max)
        {
            maxLength = max;
            return this;
        }

        IColumnMappingBuilder IColumnMappingBuilder.LoadOnly()
        {
            direction = ColumnDirection.FromDatabase;
            return this;
        }

        IColumnMappingBuilder IColumnMappingBuilder.SaveOnly()
        {
            direction = ColumnDirection.ToDatabase;
            return this;
        }

        IColumnMappingBuilder IColumnMappingBuilder.CustomPropertyHandler(IPropertyHandler propertyHandler)
        {
            PropertyHandler = propertyHandler;
            return this;
        }

        public void Validate()
        {
            if ((direction == ColumnDirection.FromDatabase || direction == ColumnDirection.Both) && PropertyHandler.CanWrite)
            {
                if (Property != null && PropertyHandler is PropertyHandler)
                {
                    // This is the most common cause of errors
                    throw new InvalidOperationException($"The mapping for column '{ColumnName}' to property '{Property.Name}' is invalid. The property has no setter, but the column mapping is not declared with SaveOnly().");
                }
                
                throw new InvalidOperationException($"The mapping for column '{ColumnName}' uses a property handler that returned false for CanWrite, and yet the column is declared as being both loaded from and saved to the database. Use `SaveOnly` if this column is intended to be saved, but not loaded from the database.");
            }
        }
    }
}