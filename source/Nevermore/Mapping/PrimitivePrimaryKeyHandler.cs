#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Nevermore.Mapping
{
    public abstract class PrimitivePrimaryKeyHandler<T> : IPrimitivePrimaryKeyHandler
    {
        public Type Type => typeof(T);

        [return: NotNullIfNotNull("id")]
        public virtual object? GetPrimitiveValue(object? id)
        {
            return id;
        }

        public abstract object FormatKey(string tableName, int key);
    }
}