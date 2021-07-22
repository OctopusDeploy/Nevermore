using System;
using System.Collections.Concurrent;
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
    public class RelationalTransactionRegistry
    {
        // Getting a typed ILog causes JIT compilation - we should only do this once
        static readonly ILog Log = LogProvider.For<RelationalTransactionRegistry>();

        readonly ConcurrentDictionary<ReadTransaction, ReadTransaction> transactions = new ConcurrentDictionary<ReadTransaction, ReadTransaction>();
        bool highNumberAlreadyLoggedAtError;

        public RelationalTransactionRegistry(SqlConnectionStringBuilder connectionString)
        {
            ConnectionString = connectionString.ToString();
            MaxPoolSize = connectionString.MaxPoolSize;
        }

        public string ConnectionString { get; }
        public int MaxPoolSize { get; }

        public void Add(ReadTransaction trn)
        {
                transactions.TryAdd(trn, trn);
                var numberOfTransactions = transactions.Count;
                if (numberOfTransactions > MaxPoolSize * 0.8)
                    Log.Info($"{numberOfTransactions} transactions active");

                if (numberOfTransactions >= MaxPoolSize || numberOfTransactions == (int)(MaxPoolSize * 0.9))
                    LogHighNumberOfTransactions(numberOfTransactions >= MaxPoolSize);
        }

        public void Remove(ReadTransaction trn)
        {
            transactions.TryRemove(trn, out _);
        }

        void LogHighNumberOfTransactions(bool reachedMax)
        {
            if (reachedMax && !highNumberAlreadyLoggedAtError)
            {
                highNumberAlreadyLoggedAtError = true;
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

        public void WriteCurrentTransactions(StringBuilder sb)
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
