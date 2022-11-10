using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Transient
{
    internal static class DbDataReaderExtensions
    {
        public static bool ReadWithRetries(this DbDataReader reader, RetryPolicy retryPolicy)
        {
            var effectiveCommandRetryPolicy = retryPolicy.LoggingRetries(nameof(DbDataReader.Read));
            return effectiveCommandRetryPolicy.ExecuteAction(reader.Read);
        }
        
        public static async Task<bool> ReadAsyncWithRetries(this DbDataReader reader, RetryPolicy retryPolicy, CancellationToken cancellationToken)
        {
            var effectiveCommandRetryPolicy = retryPolicy.LoggingRetries(nameof(DbDataReader.ReadAsync));
            return await effectiveCommandRetryPolicy.ExecuteActionAsync(async () => await reader.ReadAsync(cancellationToken));
        }
        
    }
}