using System;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Diagnositcs;
using Nito.Disposables;

namespace Nevermore.Advanced.Concurrency
{
    class LogOnlyConcurrencyHandler : ITransactionConcurrencyHandler
    {
        static readonly ILog Log = LogProvider.For<LogOnlyConcurrencyHandler>();
        
        readonly SemaphoreSlim semaphore = new(1, 1);

        public IDisposable Lock()
        {
            if (!semaphore.Wait(TimeSpan.Zero))
            {
                Log.WarnFormat("Concurrent query execution detected. Stacktrace: {0}", Environment.StackTrace);
                return NoopDisposable.Instance;
            }
            
            return new Disposable(() => semaphore.Release());
        }

        public async Task<IDisposable> LockAsync(CancellationToken cancellationToken)
        {
            if (!await semaphore.WaitAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false))
            {
                Log.WarnFormat("Concurrent query execution detected. Stacktrace: {0}", Environment.StackTrace);
                return NoopDisposable.Instance;
            }
            
            return new Disposable(() => semaphore.Release());
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }
    }
}