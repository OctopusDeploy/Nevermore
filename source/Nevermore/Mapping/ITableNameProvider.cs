using System;

namespace Nevermore.Mapping
{
    public interface ITableNameResolver
    {
        string GetTableNameFor(Type documentType);
    }
}