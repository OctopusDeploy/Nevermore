#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace Nevermore.Mapping
{
    public interface IPrimitivePrimaryKeyHandler : IPrimaryKeyHandler
    {
        [return: NotNullIfNotNull("id")]
        object? GetPrimitiveValue(object? id);

        object FormatKey(string tableName, int key);
    }
}