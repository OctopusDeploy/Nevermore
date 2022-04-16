using System.Data.Common;
using System.Diagnostics;
using Nevermore.Transient;

namespace Nevermore.Diagnostics.Events
{
    public static class DiagnosticSources
    {
        public static class Retry
        {
            static readonly DiagnosticSource Source = new DiagnosticListener("Nevermore.Retry");

            public static void OwnedConnectionClosed(DbCommand command, RetryPolicy retryPolicy, string operationName)
            {
                if (Source.IsEnabled(nameof(OwnedConnectionClosed)))
                {
                    Source.Write(nameof(OwnedConnectionClosed), new
                    {
                        Command = command,
                        RetryPolicy = retryPolicy,
                        OperationName = operationName
                    });
                }
            }

            public static void ConnectionOpened(DbConnection connection, RetryPolicy retryPolicy)
            {
                if (Source.IsEnabled(nameof(ConnectionOpened)))
                {
                    Source.Write(nameof(ConnectionOpened), new
                    {
                        Connection = connection,
                        RetryPolicy = retryPolicy
                    });
                }
            }

            public static void ConnectionReopened(DbCommand command, RetryPolicy retryPolicy)
            {
                if (Source.IsEnabled(nameof(ConnectionReopened)))
                {
                    Source.Write(nameof(ConnectionReopened), new
                    {
                        Command = command,
                        RetryPolicy = retryPolicy
                    });
                }
            }
        }
    }
}