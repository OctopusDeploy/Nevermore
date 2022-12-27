using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace Nevermore.IntegrationTests.Chaos
{
    public class ChaosSqlCommand : DbCommand
    {
        readonly DbCommand wrappedCommand;
        readonly double chaosFactor;
        readonly double closeConnectionChaosFactor = 0.5;
        static readonly Random ChaosGenerator = new();

        public ChaosSqlCommand(DbCommand wrappedCommand, double chaosFactor)
        {
            this.wrappedCommand = wrappedCommand;
            this.chaosFactor = chaosFactor;
        }

        void MakeSomeChaos()
        {
            if (Debugger.IsAttached) return; // No chaos when debugging thanks!
            if (ChaosGenerator.NextDouble() < chaosFactor)
            {
                if (ChaosGenerator.NextDouble() < closeConnectionChaosFactor)
                    wrappedCommand.Connection?.Close();
                throw new TimeoutException("You made the chaos monkey angry...");
            }
        }

        public override void Cancel()
        {
            wrappedCommand.Cancel();
        }

        protected override DbParameter CreateDbParameter()
        {
            return wrappedCommand.CreateParameter();
        }

        public override int ExecuteNonQuery()
        {
            return wrappedCommand.ExecuteNonQuery();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            MakeSomeChaos();
            return wrappedCommand.ExecuteReader(behavior);
        }

        public override object ExecuteScalar()
        {
            MakeSomeChaos();
            return wrappedCommand.ExecuteScalar();
        }

        public override void Prepare()
        {
            wrappedCommand.Prepare();
        }

        public override string CommandText
        {
            get => wrappedCommand.CommandText;
            set => wrappedCommand.CommandText = value;
        }

        public override int CommandTimeout
        {
            get => wrappedCommand.CommandTimeout;
            set => wrappedCommand.CommandTimeout = value;
        }

        public override CommandType CommandType
        {
            get => wrappedCommand.CommandType;
            set => wrappedCommand.CommandType = value;
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get => wrappedCommand.UpdatedRowSource;
            set => wrappedCommand.UpdatedRowSource = value;
        }

        protected override DbConnection DbConnection
        {
            get => wrappedCommand.Connection;
            set => wrappedCommand.Connection = value;
        }

        protected override DbParameterCollection DbParameterCollection => wrappedCommand.Parameters;

        protected override DbTransaction DbTransaction
        {
            get => wrappedCommand.Transaction;
            set => wrappedCommand.Transaction = value;
        }

        public override bool DesignTimeVisible
        {
            get => wrappedCommand.DesignTimeVisible;
            set => wrappedCommand.DesignTimeVisible = value;
        }
    }
}