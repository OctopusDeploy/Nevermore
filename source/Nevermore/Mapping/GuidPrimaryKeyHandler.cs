using System;

namespace Nevermore.Mapping
{
    class GuidPrimaryKeyHandler : PrimitivePrimaryKeyHandler<Guid>
    {
        public override object FormatKey(string tableName, int key)
        {
            return key;
        }
    }
}