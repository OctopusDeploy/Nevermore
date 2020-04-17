using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nevermore.Transient;

namespace Nevermore
{
    public static class TransientFaultHandling
    {
        static readonly string DefaultExponentialStrategyName = "default exponential";
        static readonly string FastIncrementalStrategyName = "fast incremental";

        public static readonly Incremental FastIncremental = new Incremental(name: FastIncrementalStrategyName, retryCount: 10,
            initialInterval: TimeSpan.FromSeconds(1),
            increment: TimeSpan.FromSeconds(1),
            firstFastRetry: true);

        public static readonly ExponentialBackoff DefaultExponentialBackoff = new ExponentialBackoff(name: DefaultExponentialStrategyName, retryCount: 4,
            minBackoff: TimeSpan.FromMilliseconds(100),
            maxBackoff: TimeSpan.FromSeconds(30),
            deltaBackoff: TimeSpan.FromSeconds(5),
            firstFastRetry: true);

        public static void InitializeRetryManager()
        {
            RetryManager.SetDefault(new RetryManager(
                new List<RetryStrategy>
                {
                    DefaultExponentialBackoff,
                    FastIncremental
                },
                defaultRetryStrategyName: DefaultExponentialStrategyName,
                defaultRetryStrategyNamesMap: new Dictionary<string, string>
                {
                    {RetryManagerSqlExtensions.DefaultStrategyConnectionTechnologyName, DefaultExponentialStrategyName},
                    {RetryManagerSqlExtensions.DefaultStrategyCommandTechnologyName, DefaultExponentialStrategyName},
                }));
        }
    }}
