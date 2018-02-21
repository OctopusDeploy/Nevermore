using System;
using System.Data;
using Nevermore.Mapping;

namespace Nevermore
{
    public class SqlCommandFactory : ISqlCommandFactory
    {
        public static readonly int DefaultCommandTimeoutSeconds = 60;

        public IDbCommand CreateCommand(IDbConnection connection, IDbTransaction transaction, string statement, CommandParameterValues args, DocumentMap mapping = null, int? commandTimeoutSeconds = null)
        {
            var command = connection.CreateCommand();

            try
            {
                command.CommandTimeout = commandTimeoutSeconds ?? DefaultCommandTimeoutSeconds;
                command.CommandText = statement;
                command.Transaction = transaction;
                args?.ContributeTo(command, mapping);
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