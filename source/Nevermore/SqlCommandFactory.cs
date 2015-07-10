using System.Data;

namespace Nevermore
{
    public class SqlCommandFactory : ISqlCommandFactory
    {
        public static readonly int DefaultCommandTimeoutSeconds = 60;

        public IDbCommand CreateCommand(IDbConnection connection, IDbTransaction transaction, string statement, CommandParameters args)
        {
            var command = connection.CreateCommand();

            try
            {
                command.CommandTimeout = DefaultCommandTimeoutSeconds;
                command.CommandText = statement;
                command.Transaction = transaction;

                if (args != null)
                {
                    args.ContributeTo(command);
                }
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