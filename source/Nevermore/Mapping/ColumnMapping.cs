using System;
using System.Reflection;
using Nevermore.Advanced.PropertyHandlers;

namespace Nevermore.Mapping
{
    public class ColumnMapping : IColumnMappingBuilder
    {
        const int DefaultPrimaryKeyIdLength = 50;
        const int DefaultMaxForeignKeyIdLength = 50;

        internal ColumnMapping(string columnName, Type type, IPropertyHandler handler, PropertyInfo property)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            PropertyHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            Property = property;

            if (Property == null)
                return;
            if (Property.Name == "Id")
            {
                MaxLength = DefaultPrimaryKeyIdLength;
            }
            else if (Property.Name.EndsWith("Id")) // Foreign keys
            {
                MaxLength = DefaultMaxForeignKeyIdLength;
            }
        }

        public string ColumnName { get; }
        public Type Type { get; }
        public IPropertyHandler PropertyHandler { get; private set; }
        public PropertyInfo Property { get; }

        public int? MaxLength { get; protected set; }
        public ColumnDirection Direction { get; protected set; }


        IColumnMappingBuilder IColumnMappingBuilder.MaxLength(int max)
        {
            MaxLength = max;
            return this;
        }

        IColumnMappingBuilder IColumnMappingBuilder.LoadOnly()
        {
            Direction = ColumnDirection.FromDatabase;
            return this;
        }

        IColumnMappingBuilder IColumnMappingBuilder.SaveOnly()
        {
            Direction = ColumnDirection.ToDatabase;
            return this;
        }

        IColumnMappingBuilder IColumnMappingBuilder.CustomPropertyHandler(IPropertyHandler propertyHandler)
        {
            SetCustomPropertyHandler(propertyHandler);
            return this;
        }

        protected virtual void SetCustomPropertyHandler(IPropertyHandler propertyHandler) => PropertyHandler = propertyHandler;

        public void Validate()
        {
            if ((Direction == ColumnDirection.FromDatabase || Direction == ColumnDirection.Both) && !PropertyHandler.CanWrite)
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