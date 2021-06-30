using System;
using System.Data;
using Microsoft.Data.SqlClient.Server;

namespace Nevermore.Mapping
{
    public sealed class GuidPrimaryKeyHandler : PrimaryKeyHandler<Guid>
    {
        public override SqlMetaData GetSqlMetaData(string name)
            =>  new SqlMetaData(name, SqlDbType.UniqueIdentifier);

        public override object GetNextKey(IKeyAllocator keyAllocator, string tableName)
        {
            return Guid.NewGuid();
        }
    }
}