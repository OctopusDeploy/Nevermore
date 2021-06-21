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

            if (!(PrimaryKeyHandler is null))
                throw new InvalidOperationException($"{nameof(KeyHandler)} has already been set to a non-identity handler.");

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

        public IIdColumnMappingBuilder KeyHandler(IPrimaryKeyHandler primaryKeyHandler)
        {
            if (!(PrimaryKeyHandler is null) && Direction == ColumnDirection.FromDatabase)
                throw new InvalidOperationException($"{nameof(KeyHandler)} can only be called with an IIdentityPrimaryKeyHandler, once {nameof(Identity)} has been called.");

            PrimaryKeyHandler = primaryKeyHandler;

            return this;
        }

        /// <summary>
        /// Set a function that when given the TableName will return key prefix string.
        /// </summary>
        /// <param name="idPrefix">The function to call back to get the prefix.</param>
        public IIdColumnMappingBuilder Prefix(Func<string, string> idPrefix)
        {
            if (Direction == ColumnDirection.FromDatabase)
                throw new InvalidOperationException($"{nameof(Prefix)} cannot be set when an identity key handler has been configured.");

            if (PrimaryKeyHandler == null)
                return KeyHandler(new StringPrimaryKeyHandler(idPrefix));

            if (PrimaryKeyHandler is IStringBasedPrimitivePrimaryKeyHandler stringIdHandler)
            {
                stringIdHandler.SetPrefix(idPrefix);
                return this;
            }

            throw new InvalidOperationException($"Cannot set the Id prefix when the PrimaryKeyHandler is of type {PrimaryKeyHandler.GetType().Name}");
        }

        /// <summary>
        /// Set a function that format a key value, given a prefix and a key number.
        /// </summary>
        /// <param name="format">The function to call back to format the id.</param>
        public IIdColumnMappingBuilder Format(Func<(string idPrefix, int key), string> format)
        {
            if (!(PrimaryKeyHandler is null) && Direction == ColumnDirection.FromDatabase)
                throw new InvalidOperationException($"{nameof(Format)} cannot be set when an identity key handler has been configured.");

            if (PrimaryKeyHandler == null)
                return KeyHandler(new StringPrimaryKeyHandler(format: format));

            if (PrimaryKeyHandler is IStringBasedPrimitivePrimaryKeyHandler stringIdHandler)
            {
                stringIdHandler.SetFormat(format);
                return this;
            }

            throw new InvalidOperationException($"Cannot set the key format when the PrimaryKeyHandler is of type {PrimaryKeyHandler.GetType().Name}");
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