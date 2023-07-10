using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Transient
{
    internal static class RetryUtil
    {
        public static void GuardConnectionIsNotNull(DbConnection connection)
        {
            if (connection == null)
                throw new InvalidOperationException("Connection property has not been initialized.");
        }

        /// <summary>
        /// Ensures the command either has an existing open connection, or we will open one for it.
        /// </summary>
        /// <returns>True if we opened the connection (indicating we own its lifetime), False if the connection was already open (indicating someone else owns its lifetime)</returns>
        public static bool EnsureValidConnection(DbConnection connection, RetryPolicy retryPolicy)
        {
            if (connection == null) return false;

            GuardConnectionIsNotNull(connection);

            if (connection.State == ConnectionState.Open) return false;

            connection.OpenWithRetry(retryPolicy);
            return true;
        }

        public static async Task<bool> EnsureValidConnectionAsync(DbConnection connection, RetryPolicy retryPolicy, CancellationToken cancellationToken)
        {
            if (connection == null) return false;

            GuardConnectionIsNotNull(connection);

            if (connection.State == ConnectionState.Open) return false;

            await connection.OpenWithRetryAsync(retryPolicy, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }
}