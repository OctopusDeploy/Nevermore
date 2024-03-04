using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using Nevermore.Diagnositcs;
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
        // Getting a typed ILog causes JIT compilation - we should only do this once
        static readonly ILog Log = LogProvider.For<RelationalTransactionRegistry>();

        readonly int maxSqlConnectionPoolSize;
        readonly Dictionary<ReadTransaction, ReadTransaction> transactions = new ();

        DateTime? lastHighNumberOfTransactionLogTime;
        public RelationalTransactionRegistry(int maxSqlConnectionPoolSize)
        {
            this.maxSqlConnectionPoolSize = maxSqlConnectionPoolSize;
        }

        public void Add(ReadTransaction trn)
        {
            int numberOfTransactions;
            lock (transactions)
            {
                transactions.TryAdd(trn, trn);
                numberOfTransactions = transactions.Count;
            }

            if (numberOfTransactions > maxSqlConnectionPoolSize * 0.8)
                Log.Info($"{numberOfTransactions} transactions active");

            if (numberOfTransactions >= maxSqlConnectionPoolSize || numberOfTransactions == (int)(maxSqlConnectionPoolSize * 0.9))
                LogHighNumberOfTransactions(numberOfTransactions >= maxSqlConnectionPoolSize);
        }

        public void Remove(ReadTransaction trn)
        {
            lock (transactions)
            {
                transactions.Remove(trn, out _);
            }
        }

        bool ShouldLogHighNumberOfTransactionsMessage() => lastHighNumberOfTransactionLogTime == null || DateTime.Now - lastHighNumberOfTransactionLogTime > TimeSpan.FromMinutes(1);

        void LogHighNumberOfTransactions(bool reachedMax)
        {
            if (reachedMax && ShouldLogHighNumberOfTransactionsMessage())
            {
                lastHighNumberOfTransactionLogTime = DateTime.Now;
                Log.Error(BuildHighNumberOfTransactionsMessage());
                return;
            }

            if (Log.IsDebugEnabled())
                Log.Debug(BuildHighNumberOfTransactionsMessage());
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
