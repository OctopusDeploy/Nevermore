using System.Data;
using Microsoft.Data.SqlClient.Server;

namespace Nevermore.Mapping
{
    public sealed class IntPrimaryKeyHandler : PrimaryKeyHandler<int>
    {
        public override SqlMetaData GetSqlMetaData(string name)
            =>  new SqlMetaData(name, SqlDbType.Int);

        public override object GetNextKey(IKeyAllocator keyAllocator, string tableName)
        {
            return keyAllocator.NextId(tableName);
        }
    }
}