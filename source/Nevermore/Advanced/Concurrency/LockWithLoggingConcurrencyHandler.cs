using System;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Diagnositcs;
using Nito.AsyncEx;

namespace Nevermore.Advanced.Concurrency
{
    class LockWithLoggingConcurrencyHandler : ITransactionConcurrencyHandler
    {
        static readonly ILog Log = LogProvider.For<LockWithLoggingConcurrencyHandler>();
        
        readonly SemaphoreSlim semaphore = new(1, 1);

        public IDisposable Lock()
        {
            // `SemaphoreSlim` counts down, so if it's 0 then there's a concurrent execution happening.
            if (semaphore.CurrentCount == 0)
            {
                Log.WarnFormat("Concurrent query execution detected. Stacktrace: {0}", Environment.StackTrace);
            }
            
            return semaphore.Lock();
        }

        public async Task<IDisposable> LockAsync(CancellationToken cancellationToken)
        {
            // `SemaphoreSlim` counts down, so if it's 0 then there's a concurrent execution happening.
            if (semaphore.CurrentCount == 0)
            {
                Log.WarnFormat("Concurrent query execution detected. Stacktrace: {0}", Environment.StackTrace);
            }
            
            return await semaphore.LockAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }
    }
}