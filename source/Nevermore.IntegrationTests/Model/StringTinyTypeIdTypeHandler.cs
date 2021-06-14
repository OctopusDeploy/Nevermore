#nullable enable
using System;
using System.Data;
using System.Data.Common;
using Nevermore.Advanced.TypeHandlers;

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
}