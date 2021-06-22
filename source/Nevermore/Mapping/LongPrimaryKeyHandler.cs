namespace Nevermore.Mapping
{
    public sealed class LongPrimaryKeyHandler : PrimaryKeyHandler<long>
    {
        public override object GetNextKey(IKeyAllocator keyAllocator, string tableName)
        {
            return keyAllocator.NextId(tableName);
        }
    }
}