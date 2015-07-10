using System;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.WindowsAzure.Common.TransientFaultHandling;
using Nevermore.Transient.Throttling;

namespace Nevermore.Transient
{
    sealed class SqlDatabaseTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        static readonly int[] SimpleTransientErrorCodes = { 20, 64, 233, 10053, 10054, 10060, 10928, 10929, 40143, 40197, 40540, 40613 };

        public bool IsTransient(Exception ex)
        {
            if (ex is TimeoutException) return true;

            var sqlException = ex as SqlException;
            if (sqlException == null) return false;

            // If this error was caused by throttling on the server we can augment the exception with more detail
            // I don't feel awesome about mutating the exception directly but it seems the most pragmatic way to add value
            var sqlErrors = sqlException.Errors.OfType<SqlError>().ToArray();
            var firstThrottlingError = sqlErrors.FirstOrDefault(x => x.Number == ThrottlingCondition.ThrottlingErrorNumber);
            if (firstThrottlingError != null)
            {
                AugmentSqlExceptionWithThrottlingDetails(firstThrottlingError, sqlException);
                return true;
            }

            // Otherwise it could be another simple transient error
            return sqlErrors.Select(x => x.Number).Intersect(SimpleTransientErrorCodes).Any();
        }

        static void AugmentSqlExceptionWithThrottlingDetails(SqlError error, SqlException sqlException)
        {
            // https://msdn.microsoft.com/en-us/library/azure/ff394106.aspx
            var throttlingCondition = ThrottlingCondition.FromError(error);
            sqlException.Data[throttlingCondition.ThrottlingMode.GetType().Name] = throttlingCondition.ThrottlingMode.ToString();
            sqlException.Data[throttlingCondition.GetType().Name] = throttlingCondition;
        }
    }
}