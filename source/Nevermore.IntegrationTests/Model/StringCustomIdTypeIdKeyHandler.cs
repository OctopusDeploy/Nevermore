#nullable enable
using System;
using System.Data;
using Microsoft.Data.SqlClient.Server;
using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    class StringCustomIdTypeIdKeyHandler<T> : IPrimaryKeyHandler
        where T : StringCustomIdType
    {
        readonly string? customPrefix;

        public StringCustomIdTypeIdKeyHandler(string? customPrefix = null)
        {
            this.customPrefix = customPrefix;
        }

        public Type Type => typeof(T);

        public SqlMetaData GetSqlMetaData(string name)
            =>  new SqlMetaData(name, SqlDbType.NVarChar, 300);

        public object? ConvertToPrimitiveValue(object? id)
        {
            if (!(id is StringCustomIdType stringCustomType))
                throw new ArgumentException($"Expected the id to be a {typeof(T).Name}");
            return stringCustomType.Value;
        }

        public object GetNextKey(IKeyAllocator keyAllocator, string tableName)
        {
            var key = keyAllocator.NextId(tableName);
            return CustomIdType<string>.Create<T>($"{customPrefix ?? tableName}s-{key}")!;
        }
    }
}