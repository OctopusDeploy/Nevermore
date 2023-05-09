using Nevermore.Diagnositcs;

namespace Nevermore.Diagnostics
{
    public class DefaultTransactionLogger : ITransactionLogger
    {
        static readonly ILog Log = LogProvider.For<DefaultTransactionLogger>();

        readonly long infoThreshold;

        public DefaultTransactionLogger(long infoThreshold = 10_000)
        {
            this.infoThreshold = infoThreshold;
        }
        
        public void Write(long duration, string transactionName)
        {
            var level = duration >= infoThreshold ? LogLevel.Info : LogLevel.Debug;
            Log.Log(level, () => $"Transaction '{transactionName}' was open for {duration}ms");
        }
    }
}