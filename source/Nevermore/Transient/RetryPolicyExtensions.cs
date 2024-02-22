using Microsoft.Extensions.Logging;
using Nevermore.Diagnostics;
using Nevermore.Util;

namespace Nevermore.Transient
{
    public static class RetryPolicyExtensions
    {
        public static RetryPolicy LoggingRetries(this RetryPolicy policy, ILogger logger, string operation)
        {
            if (policy == null || policy == RetryPolicy.NoRetry) return policy;

            policy.Retrying += (sender, args) =>
            {
                // Change context to the System so we don't become reentrant into ServerTask logging
                logger.RetryingOperation(operation, args.CurrentRetryCount, args.Delay, args.LastException.GetErrorSummary());
            };

            return policy;
        }
    }
}