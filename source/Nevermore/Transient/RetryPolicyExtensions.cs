using Nevermore.Diagnositcs;
using Nevermore.Util;

namespace Nevermore.Transient
{
    public static class RetryPolicyExtensions
    {
        private static readonly ILog log = LogProvider.GetLogger(typeof(RetryPolicyExtensions));

        public static RetryPolicy LoggingRetries(this RetryPolicy policy, string operation)
        {
            if (policy == null || policy == RetryPolicy.NoRetry) return policy;

            policy.Retrying += (sender, args) =>
            {
                // Change context to the System so we don't become reentrant into ServerTask logging
                log.WarnFormat("{0} attempt #{1} faulted, retrying in {2}: {3} ", operation, args.CurrentRetryCount, args.Delay, args.LastException.GetErrorSummary());
            };

            return policy;
        }
    }
}