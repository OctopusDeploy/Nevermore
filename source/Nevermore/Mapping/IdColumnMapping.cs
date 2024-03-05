#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Nevermore.Mapping
{
    public class IdColumnMapping : ColumnMapping
    {
        internal IdColumnMapping(IdColumnMappingBuilder idColumn, IPrimaryKeyHandler primaryKeyHandler)
            : base(idColumn.ColumnName, idColumn.Type, idColumn.PropertyHandler, idColumn.Property)
        {
            IsIdentity = idColumn.IsIdentity;
            PrimaryKeyHandler = primaryKeyHandler;
            Direction = idColumn.Direction;
            MaxLength = idColumn.MaxLength;
        }

        public bool IsIdentity { get; }

        public IPrimaryKeyHandler PrimaryKeyHandler { get; }
        
        static readonly HashSet<SqlDbType> ValidIdentityTypes = new ()
        {
            SqlDbType.SmallInt,
            SqlDbType.Int,
            SqlDbType.BigInt
        };

        void ValidateForIdentityUse()
        {
            if (!IsIdentity)
                return;
            if (!ValidIdentityTypes.Contains(PrimaryKeyHandler.GetSqlMetaData(ColumnName).SqlDbType))
                throw new InvalidOperationException($"The type {Type.Name} is not supported for Identity columns. Identity columns must be one of 'short', 'int' or 'long'.");
        }
    }

    public class IdColumnMappingBuilder : ColumnMapping, IIdColumnMappingBuilder
    {
        internal IdColumnMappingBuilder(string columnName, Type type, IPropertyHandler handler, PropertyInfo property) : base(columnName, type, handler, property)
        {
        }

        public bool IsIdentity { get; private set; }

        IPrimaryKeyHandler? PrimaryKeyHandler { get; set; }

        /// <inheritdoc cref="IIdColumnMappingBuilder"/>
        public IIdColumnMappingBuilder Identity()
        {
            IsIdentity = true;
            Direction = ColumnDirection.FromDatabase;

            return this;
        }

        public IIdColumnMappingBuilder KeyHandler(IPrimaryKeyHandler primaryKeyHandler)
        {
            PrimaryKeyHandler = primaryKeyHandler;
            return this;
        }

        protected override void SetCustomPropertyHandler(IPropertyHandler propertyHandler)
        {
            if (Direction == ColumnDirection.FromDatabase)
                throw new InvalidOperationException("Unable to configure an Identity Id column with a custom PropertyHandler");

            base.SetCustomPropertyHandler(propertyHandler);
        }

        public IdColumnMapping Build(IPrimaryKeyHandlerRegistry primaryKeyHandlerRegistry)
        {
            var primaryKeyHandler = PrimaryKeyHandler;
            if (primaryKeyHandler is null)
                primaryKeyHandler = primaryKeyHandlerRegistry.Resolve(Type);

            if (primaryKeyHandler is null)
                throw new InvalidOperationException($"Unable to determine a primary key handler for type {Type.Name}. This could happen if the custom PrimaryKeyHandlers are not registered prior to registering the DocumentMaps");

            var mapping = new IdColumnMapping(this, primaryKeyHandler);
            return mapping;
        }
    }
}