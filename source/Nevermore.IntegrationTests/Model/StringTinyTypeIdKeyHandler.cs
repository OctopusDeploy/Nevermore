#nullable enable
using System;
using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
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
            return TinyType<string>.Create<T>($"{tableName}s-{key}")!;
        }

        public void SetIdPrefix(Func<(string tableName, int key), string> idPrefix)
        {
            throw new NotImplementedException();
        }
    }
}