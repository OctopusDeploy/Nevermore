using System;

namespace Nevermore.Mapping
{
    public sealed class GuidPrimaryKeyHandler : PrimaryKeyHandler<Guid>
    {
        public override object GetNextKey(IKeyAllocator keyAllocator, string tableName)
        {
            return Guid.NewGuid();
        }
    }
}