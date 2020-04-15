using System;
using System.Reflection;

namespace Nevermore.Mapping
{
    public class ColumnMapping : IColumnMappingBuilder
    {
        const int MaxStringLengthByDefault = 200;
        const int DefaultPrimaryKeyIdLength = 50;
        const int DefaultMaxForeignKeyIdLength = 50;
        const int DefaultMaxEnumLength = 50;
        bool isReadOnly;
        int? maxLength;
        bool isNullable;

        internal ColumnMapping(string columnName, Type type, IPropertyReaderWriter readerWriter)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            ReaderWriter = readerWriter ?? throw new ArgumentNullException(nameof(readerWriter));
        }
        
        internal ColumnMapping(string columnName, PropertyInfo property)
        {
            Property = property;
            Type = property.PropertyType;
            ColumnName = columnName ?? property.Name;
            ReaderWriter = PropertyReaderFactory.Create<object>(property.DeclaringType, property.Name);

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
        public IPropertyReaderWriter ReaderWriter { get; }
        public PropertyInfo Property { get; }

        public bool IsReadOnly => isReadOnly;
        public bool IsNullable => isNullable;
        public int MaxLength => maxLength ?? MaxStringLengthByDefault;

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

        IColumnMappingBuilder IColumnMappingBuilder.ReadOnly()
        {
            isReadOnly = true;
            return this;
        }
    }
}