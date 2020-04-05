using System;
using System.Data;
using Nevermore.Mapping;

namespace Nevermore
{
    public class SqlCommandFactory : ISqlCommandFactory
    {
        readonly RelationalStoreConfiguration relationalStoreConfiguration;
        public static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromSeconds(60);

        public SqlCommandFactory(RelationalStoreConfiguration relationalStoreConfiguration)
        {
            this.relationalStoreConfiguration = relationalStoreConfiguration;
        }

        public IDbCommand CreateCommand(IDbConnection connection, IDbTransaction transaction, string statement, CommandParameterValues args, DocumentMap mapping = null, TimeSpan? commandTimeout = null)
        {
            var command = connection.CreateCommand();

            try
            {
                command.CommandTimeout = (int)(commandTimeout ?? DefaultCommandTimeout).TotalSeconds;
                command.CommandText = statement;
                command.Transaction = transaction;
                args?.ContributeTo(command, relationalStoreConfiguration, mapping);
                return command;
            }
            catch
            {
                command.Dispose();
                throw;
            }
        }
    }
}