using System;

namespace Nevermore.Mapping
{
    public interface IStringBasedPrimitivePrimaryKeyHandler : IPrimitivePrimaryKeyHandler
    {
        void SetIdPrefix(Func<(string tableName, int key), string> idPrefix);
    }
}