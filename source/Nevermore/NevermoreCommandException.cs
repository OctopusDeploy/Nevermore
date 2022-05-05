using System;
using System.Data.Common;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Nevermore
{
    public class NevermoreCommandException : Exception
    {
        readonly ITransactionDiagnostic transactionDiagnostic;

        internal NevermoreCommandException(DbCommand command, ITransactionDiagnostic transactionDiagnostic, SqlException innerException)
            : base($"Error while executing SQL command in transaction '{transactionDiagnostic.Name}': {innerException.Message}{Environment.NewLine}The command being executed was:{Environment.NewLine}{command.CommandText}", innerException)
        {
            Command = command;
            this.transactionDiagnostic = transactionDiagnostic;
        }

        public DbCommand Command { get; }
        public byte Class => SqlException.Class;
        public Guid ClientConnectionId => SqlException.ClientConnectionId;
        public SqlErrorCollection Errors => SqlException.Errors;
        public int LineNumber => SqlException.LineNumber;
        public int Number => SqlException.Number;
        public string Procedure => SqlException.Procedure;
        public string Server => SqlException.Server;
        public override string Source => SqlException.Source;
        public byte State => SqlException.State;

        public void WriteCurrentTransactions(StringBuilder output) => transactionDiagnostic.WriteCurrentTransactions(output);

        SqlException SqlException => InnerException as SqlException;

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine(Message);
            if (Number is 1205 or 1222 or -2)
            {
                builder.AppendLine("Current transactions: ");
                WriteCurrentTransactions(builder);
            }
            return builder.ToString();
        }
    }
}