namespace Nevermore.Mapping
{
    class LongPrimaryKeyHandler : PrimitivePrimaryKeyHandler<long>
    {
        public override object FormatKey(string tableName, int key)
        {
            return key;
        }
    }
}