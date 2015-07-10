using System;
using Microsoft.WindowsAzure.Common.TransientFaultHandling;

namespace Nevermore.Transient
{
    public static class RetryPolicyExtensions
    {
        public static RetryPolicy LoggingRetries(this RetryPolicy policy, string operation)
        {
            if (policy == null || policy == RetryPolicy.NoRetry) return policy;

            //TODO: Provide a way to inject a logger
            policy.Retrying += (sender, args) => Console.WriteLine("{0} attempt #{1} faulted, retrying in {2}: {3} ", operation, args.CurrentRetryCount, args.Delay, args.LastException.GetErrorSummary());

            return policy;
        }
    }
}