using System;
using System.Data;
using System.Data.Common;
using Nevermore.Advanced.TypeHandlers;
using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    class StringTinyTypeIdTypeHandler<T> : ITypeHandler where T : TinyType<string>
    {
        public bool CanConvert(Type objectType)
        {
            return objectType == typeof(T);
        }

        public object? ReadDatabase(DbDataReader reader, int columnIndex)
        {
            if (reader.IsDBNull(columnIndex)) return null;
            var value = reader.GetString(columnIndex);
            if (string.IsNullOrWhiteSpace(value)) return null;
            return TinyType<string>.Create<T>(value);
        }

        public void WriteDatabase(DbParameter parameter, object value)
        {
            parameter.DbType = DbType.String;
            if (value is T wrapped)
            {
                parameter.Value = wrapped.Value;
            }
            else
            {
                parameter.Value = null;
            }
        }
    }

    class StringTinyTypeIdKeyHandler<T> : IStringBasedPrimitivePrimaryKeyHandler
        where T : StringTinyType
    {
        public Type Type => typeof(T);

        public object? GetPrimitiveValue(object? id)
        {
            if (!(id is StringTinyType stringTinyType))
                throw new ArgumentException($"Expected the id to be a {typeof(T).Name}");
            return stringTinyType.Value;
        }

        public object FormatKey(string tableName, int key)
        {
            return TinyType<string>.Create<T>($"{tableName}s-{key}");
        }

        public void SetIdPrefix(Func<(string tableName, int key), string> idPrefix)
        {
            throw new NotImplementedException();
        }
    }
}