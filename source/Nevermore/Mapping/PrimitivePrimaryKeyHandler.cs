#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Nevermore.Mapping
{
    public abstract class PrimaryKeyHandler<T> : IPrimaryKeyHandler
    {
        public virtual Type Type => typeof(T);

        [return: NotNullIfNotNull("id")]
        public virtual object? ConvertToPrimitiveValue(object? id)
        {
            return id;
        }

        public abstract object GetNextKey(IKeyAllocator keyAllocator, string tableName);
    }
}