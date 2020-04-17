using System.Data;

namespace Nevermore.Advanced
{
    public static class NevermoreDefaults
    {
        public const RetriableOperation RetriableOperations = RetriableOperation.Select | RetriableOperation.Delete;
        public const IsolationLevel IsolationLevel = System.Data.IsolationLevel.ReadCommitted;

        // Increase the default connection timeout to try and prevent transaction.Commit() to timeout on slower SQL Servers.
        public const int DefaultConnectTimeoutSeconds = 60 * 5; 

        public const int DefaultConnectRetryCount = 3;
        public const int DefaultConnectRetryInterval = 10;
        
        public const int DefaultKeyBlockSize = 20;

        public const int LargeDocumentCutoffChars = 1024;
    }
}