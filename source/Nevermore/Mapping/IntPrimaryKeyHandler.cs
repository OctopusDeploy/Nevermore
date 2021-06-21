namespace Nevermore.Mapping
{
    class IntPrimaryKeyHandler : PrimaryKeyHandler<int>
    {
        public override object GetNextKey(IKeyAllocator keyAllocator, string tableName)
        {
            return keyAllocator.NextId(tableName);
        }
    }
}