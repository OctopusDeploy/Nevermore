using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace Nevermore.Advanced
{
    public class ThreadSafeAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        readonly Func<IAsyncEnumerable<T>> innerFunc;
        readonly LockWithLoggingConcurrencyHandler _lockWithLoggingConcurrencyHandler;

        public ThreadSafeAsyncEnumerable(IAsyncEnumerable<T> inner, LockWithLoggingConcurrencyHandler lockWithLoggingConcurrencyHandler) : this(() => inner, lockWithLoggingConcurrencyHandler)
        {
        }

        public ThreadSafeAsyncEnumerable(Func<IAsyncEnumerable<T>> innerFunc, LockWithLoggingConcurrencyHandler lockWithLoggingConcurrencyHandler)
        {
            this.innerFunc = innerFunc;
            this._lockWithLoggingConcurrencyHandler = lockWithLoggingConcurrencyHandler;
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
        {
            using var mutex = await _lockWithLoggingConcurrencyHandler.LockAsync(cancellationToken).ConfigureAwait(false);
            var inner = innerFunc();
            await foreach (var item in inner.WithCancellation(cancellationToken).ConfigureAwait(false)) yield return item;
        }
    }
}