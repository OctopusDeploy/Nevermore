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

        public void SetPrefix(Func<string, string> idPrefix)
        {
            throw new NotImplementedException();
        }

        public void SetFormat(Func<(string idPrefix, int key), string> format)
        {
            throw new NotImplementedException();
        }
    }
}