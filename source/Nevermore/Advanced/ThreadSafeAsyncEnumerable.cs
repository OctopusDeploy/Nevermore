using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Advanced
{
    public class ThreadSafeAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        readonly Func<IAsyncEnumerable<T>> innerFunc;
        readonly ITransactionConcurrencyHandler transactionConcurrencyHandler;

        public ThreadSafeAsyncEnumerable(IAsyncEnumerable<T> inner, ITransactionConcurrencyHandler transactionConcurrencyHandler)
            : this(() => inner, transactionConcurrencyHandler)
        {
        }

        public ThreadSafeAsyncEnumerable(Func<IAsyncEnumerable<T>> innerFunc, ITransactionConcurrencyHandler transactionConcurrencyHandler)
        {
            this.innerFunc = innerFunc;
            this.transactionConcurrencyHandler = transactionConcurrencyHandler;
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
        {
            using var mutex = await transactionConcurrencyHandler.LockAsync(cancellationToken).ConfigureAwait(false);
            var inner = innerFunc();
            await foreach (var item in inner.WithCancellation(cancellationToken).ConfigureAwait(false)) yield return item;
        }
    }
}