using System;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Diagnositcs;

namespace Nevermore.Advanced
{
    /// <summary>
    ///     This class provides a best-effort deadlock detection mechanism. It will identify re-entrant calls from the same
    ///     task (if there is a task) or the same thread (if there is no task). While it does not _guarantee_ deadlock
    ///     detection,
    ///     it does provide a pretty good guarantee that _if_ a DeadlockException is thrown then there was almost certainly
    ///     going to be a deadlock. In other words: very few false positives; probably some false negatives; better than
    ///     nothing.
    /// </summary>
    public class DeadlockAwareLock : IDisposable
    {
        static readonly ILog Log = LogProvider.For<DeadlockAwareLock>();
        
        readonly SemaphoreSlim semaphore = new(1, 1);

        readonly bool logConcurrentExecution;

        public DeadlockAwareLock(bool logConcurrentExecution)
        {
            this.logConcurrentExecution = logConcurrentExecution;
        }

        public void Wait()
        {
            // `SemaphoreSlim` counts down, so if it's 0 then there's a concurrent execution happening.
            if (logConcurrentExecution && semaphore.CurrentCount == 0)
            {
                Log.Warn("Concurrent query execution detected while waiting for lock");
            }
                
            semaphore.Wait();
        }

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            // `SemaphoreSlim` counts down, so if it's 0 then there's a concurrent execution happening.
            if (logConcurrentExecution && semaphore.CurrentCount == 0)
            {
                Log.Warn("Concurrent query execution detected while waiting for lock");
            }
            
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Release()
        {
            semaphore.Release();
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }
    }
}