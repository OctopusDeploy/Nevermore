using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nevermore.Mapping
{
    public class IdColumnMapping : ColumnMapping, IIdColumnMappingBuilder
    {
        static readonly HashSet<Type> ValidIdentityTypes = new HashSet<Type>
        {
            typeof(short),
            typeof(int),
            typeof(long)
        };

        bool hasCustomPropertyHandler;

        internal IdColumnMapping(string columnName, Type type, IPropertyHandler handler, PropertyInfo property)
            : base(columnName, type, handler, property)
        { }

        public bool IsIdentity { get; private set; }

        /// <inheritdoc cref="IIdColumnMappingBuilder"/>
        public IIdColumnMappingBuilder Identity()
        {
            if (!ValidIdentityTypes.Contains(Type))
                throw new InvalidOperationException($"The type {Type.Name} is not supported for Identity columns. Identity columns must be one of 'short', 'int' or 'long'.");

            if (hasCustomPropertyHandler)
                throw new InvalidOperationException("Unable to configure an Identity Id column with a custom PropertyHandler");

            IsIdentity = true;
            Direction = ColumnDirection.FromDatabase;

            return this;
        }

        protected override void SetCustomPropertyHandler(IPropertyHandler propertyHandler)
        {
            if (IsIdentity)
                throw new InvalidOperationException("Unable to configure an Identity Id column with a custom PropertyHandler");

            hasCustomPropertyHandler = true;
            base.SetCustomPropertyHandler(propertyHandler);
        }
    }
}