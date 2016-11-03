namespace Nevermore.Mapping
{
    public interface IKeyAllocator
    {
        void Reset();
        int NextId(string tableName);
    }
}