using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Nevermore
{
    public class ColumnMapping
    {
        public const int DefaultMaxStringLength = 200;
        public const int DefaultMaxIdLength = 50;
        public const int DefaultMaxEnumLength = 50;
        // Thumbprints today are 40 (SHA-1), 128 this allows room for alternative hash algorithms
        public const int DefaultMaxThumbprintLength = 128;
        // Theoretical maximum Uri is ~2048 but Nuget feed Uris will be shorter
        public const int DefaultMaxUriLength = 512;

        DbType? dbType;
        int maxLength;

        public ColumnMapping(string columnName, DbType dbType, IPropertyReaderWriter<object> readerWriter)
        {
            if (columnName == null)
                throw new ArgumentNullException("columnName");
            if (readerWriter == null)
                throw new ArgumentNullException("readerWriter");

            this.dbType = dbType;
            ColumnName = columnName;
            ReaderWriter = readerWriter;
        }

        public ColumnMapping(PropertyInfo property)
        {
            Property = property;
            ColumnName = Property.Name;
            ReaderWriter = PropertyReaderFactory.Create<object>(property.DeclaringType, property.Name);

            if (property.PropertyType.IsGenericType && typeof(Nullable<>).IsAssignableFrom(property.PropertyType.GetGenericTypeDefinition()))
            {
                IsNullable = true;
            }

            if (property.PropertyType == typeof(string) && property.Name.EndsWith("Id"))
            {
                DbType = DbType.String;
                if (maxLength == 0)
                {
                    MaxLength = DefaultMaxIdLength;
                }
            }

            if (property.PropertyType.IsEnum)
            {
                MaxLength = DefaultMaxEnumLength;
                DbType = DbType.String;
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
    }
}