namespace Nevermore
{
    public interface IKeyAllocator
    {
        void Reset();
        int NextId(string tableName);
    }
}