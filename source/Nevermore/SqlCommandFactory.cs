using System;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Nevermore.Advanced;
using Nevermore.Advanced.TypeHandlers;
using Nevermore.Mapping;

namespace Nevermore
{
    public class SqlCommandFactory : ISqlCommandFactory
    {
        public static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromSeconds(60);

        public DbCommand CreateCommand(DbConnection connection, DbTransaction transaction, string statement, CommandParameterValues args, ITypeHandlerRegistry typeHandlers, DocumentMap mapping = null, TimeSpan? commandTimeout = null)
        {
            var command = connection.CreateCommand();

            // if (command is SqlCommand sqlCommand)
            // {
            //     sqlCommand.RetryLogicProvider = new MyRetryLogicProvider();
            // }

            try
            {
                command.CommandTimeout = (int)(commandTimeout ?? DefaultCommandTimeout).TotalSeconds;
                command.CommandText = statement;
                command.Transaction = transaction;
                args?.ContributeTo(command, typeHandlers, mapping);
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