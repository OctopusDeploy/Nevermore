#nullable enable
using System;
using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    class StringCustomIdTypeIdKeyHandler<T> : IPrimaryKeyHandler
        where T : StringCustomIdType
    {
        readonly string? customPrefix;
        public Type Type => typeof(T);

        public StringCustomIdTypeIdKeyHandler(string? customPrefix = null)
        {
            this.customPrefix = customPrefix;
        }

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