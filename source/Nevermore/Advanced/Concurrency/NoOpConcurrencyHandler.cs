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

        public Task<IDisposable> LockAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IDisposable>(NoopDisposable.Instance);
        }

        public void Dispose()
        {
        }
    }
}