#nullable enable
using System;
using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    class StringCustomIdTypeIdKeyHandler<T> : IStringBasedPrimitivePrimaryKeyHandler
        where T : StringCustomIdType
    {
        public Type Type => typeof(T);

        public object? ConvertToPrimitiveValue(object? id)
        {
            if (!(id is StringCustomIdType stringCustomType))
                throw new ArgumentException($"Expected the id to be a {typeof(T).Name}");
            return stringCustomType.Value;
        }

        public object FormatKey(string tableName, int key)
        {
            return CustomIdType<string>.Create<T>($"{GetPrefix(tableName)}-{key}")!;
        }

        public void SetPrefix(Func<string, string> idPrefix)
        {
            throw new NotImplementedException();
        }

        public string GetPrefix(string tableName)
        {
            return $"{tableName}s";
        }

        public void SetFormat(Func<(string idPrefix, int key), string> format)
        {
            throw new NotImplementedException();
        }
    }
}