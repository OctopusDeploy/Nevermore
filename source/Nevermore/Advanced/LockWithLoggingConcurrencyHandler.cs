using System;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Diagnositcs;
using Nito.AsyncEx;
using Nito.Disposables;

namespace Nevermore.Advanced
{
    public interface ITransactionConcurrencyHandler : IDisposable
    {
        IDisposable Wait();
        
        Task<IDisposable> WaitAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    ///     This class provides a best-effort deadlock detection mechanism. It will identify re-entrant calls from the same
    ///     task (if there is a task) or the same thread (if there is no task). While it does not _guarantee_ deadlock
    ///     detection,
    ///     it does provide a pretty good guarantee that _if_ a DeadlockException is thrown then there was almost certainly
    ///     going to be a deadlock. In other words: very few false positives; probably some false negatives; better than
    ///     nothing.
    /// </summary>
    public class LockWithLoggingConcurrencyHandler : ITransactionConcurrencyHandler
    {
        static readonly ILog Log = LogProvider.For<LockWithLoggingConcurrencyHandler>();
        
        readonly SemaphoreSlim semaphore = new(1, 1);

        public IDisposable Wait()
        {
            // `SemaphoreSlim` counts down, so if it's 0 then there's a concurrent execution happening.
            if (semaphore.CurrentCount == 0)
            {
                Log.Warn("Concurrent query execution detected while waiting for lock");
            }
            
            return semaphore.Lock();
        }

        public async Task<IDisposable> WaitAsync(CancellationToken cancellationToken)
        {
            // `SemaphoreSlim` counts down, so if it's 0 then there's a concurrent execution happening.
            if (semaphore.CurrentCount == 0)
            {
                Log.Warn("Concurrent query execution detected while waiting for lock");
            }
            
            return await semaphore.LockAsync(cancellationToken).ConfigureAwait(false);
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
    
    public class LockOnlyConcurrencyHandler : ITransactionConcurrencyHandler
    {
        readonly SemaphoreSlim semaphore = new(1, 1);

        public IDisposable Wait()
        {
            return semaphore.Lock();
        }

        public async Task<IDisposable> WaitAsync(CancellationToken cancellationToken)
        {
            return await semaphore.LockAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }
    }
    
    public class LogOnlyConcurrencyHandler : ITransactionConcurrencyHandler
    {
        static readonly ILog Log = LogProvider.For<LogOnlyConcurrencyHandler>();
        
        readonly SemaphoreSlim semaphore = new(1, 1);

        public IDisposable Wait()
        {
            if (!semaphore.Wait(TimeSpan.Zero))
            {
                Log.Warn("Concurrent query execution detected while waiting for lock");

                return NoopDisposable.Instance;
            }
            
            return new ConcurrencyDisposable(() => semaphore.Release());
        }

        public async Task<IDisposable> WaitAsync(CancellationToken cancellationToken)
        {
            if (!await semaphore.WaitAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false))
            {
                Log.Warn("Concurrent query execution detected while waiting for lock");
                
                return NoopDisposable.Instance;
            }
            
            return new ConcurrencyDisposable(() => semaphore.Release());
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }
    }

    public class ConcurrencyDisposable : IDisposable
    {
        readonly Action disposeAction;
        
        public ConcurrencyDisposable(Action disposeAction)
        {
            this.disposeAction = disposeAction;
        }
        
        public void Dispose()
        {
            disposeAction();
        }
    }
}