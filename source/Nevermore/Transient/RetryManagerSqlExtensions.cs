using System;
using Microsoft.WindowsAzure.Common.TransientFaultHandling;

namespace Nevermore.Transient
{
    public static class RetryManagerSqlExtensions
    {
        public static readonly string DefaultStrategyCommandTechnologyName = "SQL";
        public static readonly string DefaultStrategyConnectionTechnologyName = "SQLConnection";

        public static RetryPolicy GetDefaultSqlCommandRetryPolicy(this RetryManager retryManager)
        {
            if (retryManager == null) throw new ArgumentNullException("retryManager");

            return new RetryPolicy(new SqlDatabaseTransientErrorDetectionStrategy(), retryManager.GetDefaultSqlCommandRetryStrategy());
        }

        public static RetryStrategy GetDefaultSqlCommandRetryStrategy(this RetryManager retryManager)
        {
            if (retryManager == null) throw new ArgumentNullException("retryManager");

            return retryManager.GetDefaultRetryStrategy(DefaultStrategyCommandTechnologyName);
        }

        public static RetryPolicy GetDefaultSqlConnectionRetryPolicy(this RetryManager retryManager)
        {
            if (retryManager == null) throw new ArgumentNullException("retryManager");

            return new RetryPolicy(new SqlDatabaseTransientErrorDetectionStrategy(), retryManager.GetDefaultSqlConnectionRetryStrategy());
        }

        public static RetryStrategy GetDefaultSqlConnectionRetryStrategy(this RetryManager retryManager)
        {
            if (retryManager == null) throw new ArgumentNullException("retryManager");

            try
            {
                return retryManager.GetDefaultRetryStrategy(DefaultStrategyConnectionTechnologyName);
            }
            catch (ArgumentOutOfRangeException)
            {
                return retryManager.GetDefaultRetryStrategy(DefaultStrategyCommandTechnologyName);
            }
        }
    }
}