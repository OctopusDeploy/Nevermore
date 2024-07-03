using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Advanced.Concurrency;

namespace Nevermore.Advanced
{
    internal class AsyncEnumerableWithConcurrencyHandling<T> : IAsyncEnumerable<T>
    {
        readonly Func<IAsyncEnumerable<T>> innerFunc;
        readonly ITransactionConcurrencyHandler transactionConcurrencyHandler;

        public AsyncEnumerableWithConcurrencyHandling(IAsyncEnumerable<T> inner, ITransactionConcurrencyHandler transactionConcurrencyHandler)
            : this(() => inner, transactionConcurrencyHandler)
        {
        }

        public AsyncEnumerableWithConcurrencyHandling(Func<IAsyncEnumerable<T>> innerFunc, ITransactionConcurrencyHandler transactionConcurrencyHandler)
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