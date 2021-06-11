#nullable enable
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

        public IPrimaryKeyHandler? PrimaryKeyHandler { get; private set; }

        /// <inheritdoc cref="IIdColumnMappingBuilder"/>
        public IIdColumnMappingBuilder Identity()
        {
            ValidateForIdentityUse();

            if (!(PrimaryKeyHandler is null) && !(PrimaryKeyHandler is IIdentityPrimaryKeyHandler))
                throw new InvalidOperationException($"{nameof(KeyHandler)} has already been set to a non-identity handler.");

            var handlerType = typeof(IdentityPrimaryKeyHandler<>).MakeGenericType(Type);
            var keyHandler = (IIdentityPrimaryKeyHandler)Activator.CreateInstance(handlerType);

            return KeyHandler(keyHandler);
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
            if (!(PrimaryKeyHandler is null) && Direction == ColumnDirection.FromDatabase && !(primaryKeyHandler is IIdentityPrimaryKeyHandler))
                throw new InvalidOperationException($"{nameof(KeyHandler)} can only be called with an IIdentityPrimaryKeyHandler, once {nameof(Identity)} has been called.");

            if (primaryKeyHandler is IIdentityPrimaryKeyHandler)
            {
                ValidateForIdentityUse();
                Direction = ColumnDirection.FromDatabase;
            }

            PrimaryKeyHandler = primaryKeyHandler;

            return this;
        }

        public IIdColumnMappingBuilder IdPrefix(Func<(string tableName, int key), string> idPrefix)
        {
            if (!(PrimaryKeyHandler is null) && Direction == ColumnDirection.FromDatabase && PrimaryKeyHandler is IIdentityPrimaryKeyHandler)
                throw new InvalidOperationException($"{nameof(IdPrefix)} cannot be set when an identity key handler has been configured.");

            return KeyHandler(new StringPrimaryKeyHandler(idPrefix));
        }

        protected override void SetCustomPropertyHandler(IPropertyHandler propertyHandler)
        {
            if (PrimaryKeyHandler is IIdentityPrimaryKeyHandler)
                throw new InvalidOperationException("Unable to configure an Identity Id column with a custom PropertyHandler");

            hasCustomPropertyHandler = true;
            base.SetCustomPropertyHandler(propertyHandler);
        }
    }
}