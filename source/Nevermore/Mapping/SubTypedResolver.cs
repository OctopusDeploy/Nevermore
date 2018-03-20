using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Nevermore.Mapping
{
    public abstract class InstanceTypeResolver
    {
        public abstract Type GetTypeFromInstance(object instance);
        public abstract Func<IDataReader, Type> TypeResolverFromReader(Func<string, int> columnOrdinal);
    }

    public class StandardTypeResolver : InstanceTypeResolver
    {
        readonly DocumentMap mapper;

        public StandardTypeResolver(DocumentMap mapper)
        {
            this.mapper = mapper;
        }

        public override Type GetTypeFromInstance(object instance)
        {
            return mapper.Type;
        }

        public override Func<IDataReader, Type> TypeResolverFromReader(Func<string, int> columnOrdinal)
        {
            return (reader) => mapper.Type;
        }
    }

    public class SubTypedResolver<TDocument, TProperty> : InstanceTypeResolver where TProperty : struct
    {
        readonly ColumnMapping column;
        readonly Dictionary<TProperty, Type> typeBuilder;

        public SubTypedResolver(ColumnMapping column, Dictionary<TProperty, Type> typeBuilder)
        {
            if (!typeof(TProperty).GetTypeInfo().IsEnum)
            {
                throw new InvalidOperationException($"The type discriminator for column {column.ColumnName} on object {typeof(TDocument).Name} must deserialize to an enum property");
            }

            this.column = column;


            this.typeBuilder = typeBuilder;
        }

        public override Type GetTypeFromInstance(object instance)
        {
            var discriminator = (TProperty)column.ReaderWriter.Read(instance);
            if (!typeBuilder.ContainsKey(discriminator))
            {
                throw new InvalidOperationException($"Unable to map provided enum {discriminator} to a sub type.");

            }

            return typeBuilder[discriminator];
        }

        public override Func<IDataReader, Type> TypeResolverFromReader(Func<string, int> columnOrdinal)
        {
            var colIndex = columnOrdinal(column.ColumnName);
            return reader => GetType(
                0 <= colIndex && colIndex < reader.FieldCount 
                ? reader[colIndex].ToString() 
                : "");
        }

        Type GetType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return typeof(TDocument);

            TProperty discriminator;
            try
            {
                discriminator = (TProperty)Enum.Parse(column.Property.PropertyType, value, true);
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException($"Unable to map provided enum {value} to a sub type.");
            }

            if (!typeBuilder.ContainsKey(discriminator))
            {
                throw new InvalidOperationException($"Unable to map provided enum {value} to a sub type.");

            }

            return typeBuilder[discriminator];
        }
    }
}