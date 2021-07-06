#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient.Server;

namespace Nevermore.Mapping
{
    public abstract class PrimaryKeyHandler<T> : IPrimaryKeyHandler
    {
        public Type Type => typeof(T);

        public abstract SqlMetaData GetSqlMetaData(string name);

        [return: NotNullIfNotNull("id")]
        public virtual object? ConvertToPrimitiveValue(object? id)
        {
            return id;
        }

        public abstract object GetNextKey(IKeyAllocator keyAllocator, string tableName);
    }
}