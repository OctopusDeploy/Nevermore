using System;
using System.Data;
using System.Diagnostics;

namespace Nevermore.IntegrationTests.Chaos
{
    public class ChaosSqlCommand : IDbCommand
    {
        readonly IDbCommand wrappedCommand;
        readonly double chaosFactor;
        static readonly Random ChaosGenerator = new Random();

        public ChaosSqlCommand(IDbCommand wrappedCommand, double chaosFactor)
        {
            this.wrappedCommand = wrappedCommand;
            this.chaosFactor = chaosFactor;
        }

        void MakeSomeChaos()
        {
            if (Debugger.IsAttached) return; // No chaos when debugging thanks!
            if (ChaosGenerator.NextDouble() < chaosFactor) throw new TimeoutException("You made the chaos monkey angry...");
        }

        public int ExecuteNonQuery()
        {
            return wrappedCommand.ExecuteNonQuery();
        }

        public IDataReader ExecuteReader()
        {
            MakeSomeChaos();
            return wrappedCommand.ExecuteReader();
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            MakeSomeChaos();
            return wrappedCommand.ExecuteReader(behavior);
        }

        public object ExecuteScalar()
        {
            MakeSomeChaos();
            return wrappedCommand.ExecuteScalar();
        }

        #region Purely Wrapped
        public void Dispose()
        {
            wrappedCommand.Dispose();
        }

        public void Prepare()
        {
            wrappedCommand.Prepare();
        }

        public void Cancel()
        {
            wrappedCommand.Cancel();
        }

        public IDbDataParameter CreateParameter()
        {
            return wrappedCommand.CreateParameter();
        }

        public IDbConnection Connection
        {
            get { return wrappedCommand.Connection; }
            set { wrappedCommand.Connection = value; }
        }

        public IDbTransaction Transaction
        {
            get { return wrappedCommand.Transaction; }
            set { wrappedCommand.Transaction = value; }
        }

        public string CommandText
        {
            get { return wrappedCommand.CommandText; }
            set { wrappedCommand.CommandText = value; }
        }

        public int CommandTimeout
        {
            get { return wrappedCommand.CommandTimeout; }
            set { wrappedCommand.CommandTimeout = value; }
        }

        public CommandType CommandType
        {
            get { return wrappedCommand.CommandType; }
            set { wrappedCommand.CommandType = value; }
        }

        public IDataParameterCollection Parameters
        {
            get { return wrappedCommand.Parameters; }
        }

        public UpdateRowSource UpdatedRowSource
        {
            get { return wrappedCommand.UpdatedRowSource; }
            set { wrappedCommand.UpdatedRowSource = value; }
        }
        #endregion
    }
}