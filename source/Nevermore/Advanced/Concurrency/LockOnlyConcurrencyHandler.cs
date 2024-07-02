using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace Nevermore.Advanced.Concurrency
{
    public class LockOnlyConcurrencyHandler : ITransactionConcurrencyHandler
    {
        readonly SemaphoreSlim semaphore = new(1, 1);

        public IDisposable Lock()
        {
            return semaphore.Lock();
        }

        public async Task<IDisposable> LockAsync(CancellationToken cancellationToken)
        {
            return await semaphore.LockAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }
    }
}