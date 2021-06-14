namespace Nevermore.Mapping
{
    class IntPrimaryKeyHandler : PrimitivePrimaryKeyHandler<int>
    {
        public override object FormatKey(string tableName, int key)
        {
            return key;
        }
    }
}