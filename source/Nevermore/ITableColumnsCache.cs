using System;

namespace Nevermore
{
    public interface ITableColumnsCache
    {
        string[] GetOrAdd(string schemaName, string tableName, Func<string, string, string[]> valueFactory);
    }
}