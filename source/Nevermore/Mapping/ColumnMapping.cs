using System;
using System.Data;
using System.Reflection;
using Nevermore.Contracts;

namespace Nevermore.Mapping
{
    public class ColumnMapping
    {
        public const int DefaultMaxLongStringLength = 2000;
        public const int DefaultMaxStringLength = 200;
        // Override in DocumentMap subclasses where necessary
        public const int DefaultPrimaryKeyIdLength = 50;
        public const int DefaultMaxForeignKeyIdLength = 50;
        public const int DefaultMaxEnumLength = 50;
        // Thumbprints today are 40 (SHA-1), 128 this allows room for alternative hash algorithms
        public const int DefaultMaxThumbprintLength = 128;
        // Theoretical maximum Uri is ~2048 but Nuget feed Uris will be shorter
        public const int DefaultMaxUriLength = 512;

        DbType? dbType;
        int maxLength;

        public ColumnMapping(string columnName, DbType dbType, IPropertyReaderWriter<object> readerWriter, bool readOnly = false)
        {
            if (columnName == null)
                throw new ArgumentNullException("columnName");
            if (readerWriter == null)
                throw new ArgumentNullException("readerWriter");

            this.dbType = dbType;
            ColumnName = columnName;
            ReaderWriter = readerWriter;
            IsReadOnly = readOnly;
        }

        public ColumnMapping(PropertyInfo property)
        {
            Property = property;
            ColumnName = Property.Name;
            ReaderWriter = PropertyReaderFactory.Create<object>(property.DeclaringType, property.Name);

            if (property.PropertyType.GetTypeInfo().IsGenericType && typeof(Nullable<>).GetTypeInfo().IsAssignableFrom(property.PropertyType.GetGenericTypeDefinition()))
            {
                IsNullable = true;
            }
            
            if (property.PropertyType == typeof(string))
            {
                DbType = DbType.String;
                if (maxLength == 0)
                {
                    if (string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase)) // Primary keys
                    {
                        MaxLength = DefaultPrimaryKeyIdLength;
                    }
                    else if (property.Name.EndsWith("Id")) // Foreign keys
                    {
                        MaxLength = DefaultMaxForeignKeyIdLength;
                    }
                }
            }

            if (property.PropertyType.GetTypeInfo().IsEnum)
            {
                MaxLength = DefaultMaxEnumLength;
                DbType = DbType.String;
            }

            if (property.PropertyType == typeof(ReferenceCollection))
            {
                DbType = DbType.String;
                MaxLength = int.MaxValue;
                ReaderWriter = new ReferenceCollectionReaderWriter(ReaderWriter);
            }
        }

        public bool IsNullable { get; set; }
        public string ColumnName { get; private set; }

        public DbType DbType
        {
            get
            {
                if (dbType == null)
                    return DbType = DatabaseTypeConverter.AsDbType(Property.PropertyType);
                return dbType.Value;
            }
            set { dbType = value; }
        }

        public int MaxLength
        {
            get
            {
                if (maxLength == 0 && (dbType == DbType.String))
                {
                    MaxLength = DefaultMaxStringLength;
                }
                return maxLength;
            }
            set { maxLength = value; }
        }

        public PropertyInfo Property { get; private set; }
        public IPropertyReaderWriter<object> ReaderWriter { get; set; }
        public bool IsReadOnly { get; set; }

        public ColumnMapping Nullable()
        {
            IsNullable = true;
            return this;
        }

        public ColumnMapping WithMaxLength(int max)
        {
            maxLength = max;
            return this;
        }

        public ColumnMapping ReadOnly()
        {
            IsReadOnly = true;
            return this;
        }
        
        public ColumnMapping WithColumnName(string columnName)
        {
            ColumnName = columnName;
            return this;
        }
    }
}