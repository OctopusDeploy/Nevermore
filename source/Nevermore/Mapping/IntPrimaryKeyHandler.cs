using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient.Server;

namespace Nevermore.Mapping
{
    public sealed class IntPrimaryKeyHandler : AsyncPrimaryKeyHandler<int>
    {
        public override SqlMetaData GetSqlMetaData(string name) => new(name, SqlDbType.Int);

        public override object GetNextKey(IKeyAllocator keyAllocator, string tableName)
        {
            return keyAllocator.NextId(tableName);
        }

        public override async ValueTask<object> GetNextKeyAsync(IKeyAllocator keyAllocator, string tableName, CancellationToken cancellationToken)
        {
            return await keyAllocator.NextIdAsync(tableName, cancellationToken).ConfigureAwait(false);
        }
    }
}