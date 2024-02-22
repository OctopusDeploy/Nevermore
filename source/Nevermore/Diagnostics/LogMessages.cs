using System;
using Microsoft.Extensions.Logging;

namespace Nevermore.Diagnostics
{
    internal static partial class LogMessages
    {
        [LoggerMessage(LogLevel.Debug, "A possible N+1 or long running transaction detected, this is a diagnostic message only does not require end-user action.\r\nStarted: {StartedAt:s}\r\nStack: {Stack}\r\n\r\n{CommandTrace}")]
        internal static partial void PossibleLongTransactionDetected(this ILogger logger, DateTimeOffset startedAt, string stack, string commandTrace);

        [LoggerMessage(LogLevel.Debug, "[{CorrelationId}] Txn {TransactionName} Cmd {SqlCommand}")]
        internal static partial void ProcessReaderStarted(this ILogger logger, string correlationId, string transactionName, string sqlCommand);

        [LoggerMessage(LogLevel.Debug, "[{CorrelationId}] Row {RowIndex} failed to be read and will be discarded")]
        internal static partial void ProcessReaderRowFailed(this ILogger logger, string correlationId, int rowIndex);

        [LoggerMessage(LogLevel.Information, "{NumberOfTransactions} transactions active")]
        internal static partial void ActiveTransactions(this ILogger logger, int numberOfTransactions);

        [LoggerMessage(LogLevel.Debug, "Exception in relational transaction '{TransactionName}'")]
        internal static partial void TransactionError(this ILogger logger, Exception ex, string transactionName);

        [LoggerMessage(LogLevel.Warning, "{Operation} attempt #{AttemptNumber} faulted, retrying in {RetryDelay}: {FailureReason}")]
        internal static partial void RetryingOperation(this ILogger logger, string operation, int attemptNumber, TimeSpan retryDelay, string failureReason);
    }
}