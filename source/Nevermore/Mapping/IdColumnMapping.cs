#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nevermore.Mapping
{
    public class IdColumnMapping : ColumnMapping
    {
        internal IdColumnMapping(string columnName, Type type, IPropertyHandler handler, PropertyInfo property, bool isIdentity, IPrimaryKeyHandler primaryKeyHandler)
            : base(columnName, type, handler, property)
        {
            IsIdentity = isIdentity;
            Direction = IsIdentity ? ColumnDirection.FromDatabase : ColumnDirection.Both;
            PrimaryKeyHandler = primaryKeyHandler;
        }

        public bool IsIdentity { get; }

        public IPrimaryKeyHandler PrimaryKeyHandler { get; }
    }

    public class IdColumnMappingBuilder : ColumnMapping, IIdColumnMappingBuilder
    {
        static readonly HashSet<Type> ValidIdentityTypes = new HashSet<Type>
        {
            typeof(short),
            typeof(int),
            typeof(long)
        };

        bool hasCustomPropertyHandler;

        internal IdColumnMappingBuilder(string columnName, Type type, IPropertyHandler handler, PropertyInfo property) : base(columnName, type, handler, property)
        {
        }

        bool IsIdentity { get; set; }

        IPrimaryKeyHandler? PrimaryKeyHandler { get; set; }

        /// <inheritdoc cref="IIdColumnMappingBuilder"/>
        public IIdColumnMappingBuilder Identity()
        {
            ValidateForIdentityUse();

            IsIdentity = true;

            return this;
        }

        void ValidateForIdentityUse()
        {
            if (!ValidIdentityTypes.Contains(Type))
                throw new InvalidOperationException($"The type {Type.Name} is not supported for Identity columns. Identity columns must be one of 'short', 'int' or 'long'.");

            if (hasCustomPropertyHandler)
                throw new InvalidOperationException("Unable to configure an Identity Id column with a custom PropertyHandler");
        }

        protected override void SetCustomPropertyHandler(IPropertyHandler propertyHandler)
        {
            if (Direction == ColumnDirection.FromDatabase)
                throw new InvalidOperationException("Unable to configure an Identity Id column with a custom PropertyHandler");

            hasCustomPropertyHandler = true;
            base.SetCustomPropertyHandler(propertyHandler);
        }

        public IdColumnMapping Build(IPrimaryKeyHandlerRegistry primaryKeyHandlerRegistry)
        {
            var primaryKeyHandler = PrimaryKeyHandler;
            if (primaryKeyHandler is null)
                primaryKeyHandler = primaryKeyHandlerRegistry.Resolve(Type);

            if (primaryKeyHandler is null)
                throw new InvalidOperationException($"Unable to determine a primary key handler for type {Type.Name}. This could happen if the custom PrimaryKeyHandlers are not registered prior to registering the DocumentMaps");

            var mapping = new IdColumnMapping(ColumnName, Type, PropertyHandler, Property, IsIdentity, primaryKeyHandler);
            return mapping;
        }
    }
}