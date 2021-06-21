#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace Nevermore.Mapping
{
    public interface IPrimitivePrimaryKeyHandler : IPrimaryKeyHandler
    {
        [return: NotNullIfNotNull("id")]
        object? ConvertToPrimitiveValue(object? id);
    }
}