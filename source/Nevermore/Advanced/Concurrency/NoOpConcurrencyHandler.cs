using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.Disposables;

namespace Nevermore.Advanced.Concurrency
{
    public class NoOpConcurrencyHandler : ITransactionConcurrencyHandler
    {
        public IDisposable Lock()
        {
            return NoopDisposable.Instance;
        }

        public async Task<IDisposable> LockAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            return NoopDisposable.Instance;
        }

        public void Dispose()
        {
        }
    }
}