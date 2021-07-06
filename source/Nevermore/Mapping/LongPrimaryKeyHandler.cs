using System.Data;
using Microsoft.Data.SqlClient.Server;

namespace Nevermore.Mapping
{
    public sealed class LongPrimaryKeyHandler : PrimaryKeyHandler<long>
    {
        public override SqlMetaData GetSqlMetaData(string name)
            =>  new SqlMetaData(name, SqlDbType.BigInt);

        public override object GetNextKey(IKeyAllocator keyAllocator, string tableName)
        {
            return keyAllocator.NextId(tableName);
        }
    }
}