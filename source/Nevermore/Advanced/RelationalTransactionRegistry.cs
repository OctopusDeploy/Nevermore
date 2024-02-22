using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

#if NETFRAMEWORK
using System.Data.SqlClient;
#else
#endif

namespace Nevermore.Advanced
{
    public interface IRelationalTransactionRegistry
    {
        void Add(ReadTransaction trn);
        void Remove(ReadTransaction trn);
        void WriteCurrentTransactions(StringBuilder sb);
    }

    public class RelationalTransactionRegistry : IRelationalTransactionRegistry
    {
        readonly int maxSqlConnectionPoolSize;
        readonly ILogger logger;
        readonly ConcurrentDictionary<ReadTransaction, ReadTransaction> transactions = new();

        DateTime? lastHighNumberOfTransactionLogTime;

        public RelationalTransactionRegistry(int maxSqlConnectionPoolSize, ILogger logger)
        {
            this.maxSqlConnectionPoolSize = maxSqlConnectionPoolSize;
            this.logger = logger;
        }

        public void Add(ReadTransaction trn)
        {
            transactions.TryAdd(trn, trn);
            var numberOfTransactions = transactions.Count;
            if (numberOfTransactions > maxSqlConnectionPoolSize * 0.8)
                logger.LogInformation("{NumberOfTransactions} transactions active", numberOfTransactions);

            if (numberOfTransactions >= maxSqlConnectionPoolSize || numberOfTransactions == (int)(maxSqlConnectionPoolSize * 0.9))
                LogHighNumberOfTransactions(numberOfTransactions >= maxSqlConnectionPoolSize);
        }

        public void Remove(ReadTransaction trn)
        {
            transactions.TryRemove(trn, out _);
        }

        bool ShouldLogHighNumberOfTransactionsMessage() => lastHighNumberOfTransactionLogTime == null || DateTime.Now - lastHighNumberOfTransactionLogTime > TimeSpan.FromMinutes(1);

        void LogHighNumberOfTransactions(bool reachedMax)
        {
            if (reachedMax && ShouldLogHighNumberOfTransactionsMessage())
            {
                lastHighNumberOfTransactionLogTime = DateTime.Now;
                logger.LogError(BuildHighNumberOfTransactionsMessage());
                return;
            }

            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug(BuildHighNumberOfTransactionsMessage());
        }

        string BuildHighNumberOfTransactionsMessage()
        {
            var sb = new StringBuilder();
            sb.AppendLine("There are a high number of transactions active. The below information may help the Octopus team diagnose the problem:");
            sb.AppendLine($"Now: {DateTime.Now:s}");
            WriteCurrentTransactions(sb);
            return sb.ToString();
        }

        public virtual void WriteCurrentTransactions(StringBuilder sb)
        {
            ReadTransaction[] copy;
            lock (transactions)
                copy = transactions.Keys.ToArray();

            foreach (var trn in copy.OrderBy(t => t.CreatedTime))
            {
                sb.AppendLine();
                trn.WriteDebugInfoTo(sb);
            }
        }
    }
}
