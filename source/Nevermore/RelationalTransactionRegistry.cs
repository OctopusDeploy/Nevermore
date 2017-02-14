using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Nevermore.Diagnositcs;

namespace Nevermore
{
    public class RelationalTransactionRegistry
    {
        readonly ILog log = LogProvider.For<RelationalTransactionRegistry>();

        readonly List<RelationalTransaction> transactions = new List<RelationalTransaction>();

        public RelationalTransactionRegistry(SqlConnectionStringBuilder connectionString)
        {
            ConnectionString = connectionString.ToString();
            MaxPoolSize = connectionString.MaxPoolSize;
        }

        public string ConnectionString { get; }
        public int MaxPoolSize { get; }

        public void Add(RelationalTransaction trn)
        {
            lock (transactions)
            {
                transactions.Add(trn);
                var numberOfTransactions = transactions.Count;
                if (numberOfTransactions > MaxPoolSize * 0.8)
                    log.Debug("{numberOfTransactions} transactions active");
                if (numberOfTransactions == MaxPoolSize || numberOfTransactions == (int)(MaxPoolSize * 0.9))
                    LogHighNumberOfTransactions();
            }
        }

        public void Remove(RelationalTransaction trn)
        {
            lock (transactions)
                transactions.Remove(trn);
        }

        void LogHighNumberOfTransactions()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("There are a high number of transactions active. The below information may help the Octopus team diagnose the problem:");
            sb.AppendLine($"Now: {DateTime.Now:s}");
            WriteCurrentTransactions(sb);
            log.Debug(sb.ToString());
        }

        public void WriteCurrentTransactions(StringBuilder sb)
        {
            RelationalTransaction[] copy;
            lock (transactions)
                copy = transactions.ToArray();

            foreach (var trn in copy.OrderBy(t => t.CreatedTime))
            {
                sb.AppendLine();
                trn.WriteDebugInfoTo(sb);
            }
        }

    }
}