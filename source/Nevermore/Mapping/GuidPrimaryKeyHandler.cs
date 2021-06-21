using System;

namespace Nevermore.Mapping
{
    class GuidPrimaryKeyHandler : PrimaryKeyHandler<Guid>
    {
        public override object GetNextKey(IKeyAllocator keyAllocator, string tableName)
        {
            return Guid.NewGuid();
        }
    }
}