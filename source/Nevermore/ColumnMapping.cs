using System;
using System.Data;
using System.Reflection;

namespace Nevermore
{
    public class ColumnMapping
    {
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
                    MaxLength = 50;           
                }
            }

            if (property.PropertyType.IsEnum)
            {
                MaxLength = 50;
                DbType = DbType.String;
            }

            if (property.PropertyType == typeof(ReferenceCollection))
            {
                DbType = DbType.String;
                MaxLength = int.MaxValue;
                ReaderWriter = new ReferenceCollectionReaderWriter(ReaderWriter);
            }
        }

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
                    MaxLength = 100;
                }
                return maxLength;
            }
            set { maxLength = value; }
        }

        public PropertyInfo Property { get; private set; }
        public IPropertyReaderWriter<object> ReaderWriter { get; set; }
    }
}