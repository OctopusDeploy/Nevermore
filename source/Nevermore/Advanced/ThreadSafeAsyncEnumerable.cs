using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Advanced.Concurrency;
using Nito.AsyncEx;

namespace Nevermore.Advanced
{
    public class ThreadSafeAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        readonly Func<IAsyncEnumerable<T>> innerFunc;
        readonly TransactionMutex transactionMutex;

        public ThreadSafeAsyncEnumerable(IAsyncEnumerable<T> inner, TransactionMutex transactionMutex) : this(() => inner, transactionMutex)
        {
        }

        public ThreadSafeAsyncEnumerable(Func<IAsyncEnumerable<T>> innerFunc, TransactionMutex transactionMutex)
        {
            this.innerFunc = innerFunc;
            this.transactionMutex = transactionMutex;
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
        {
            using var mutex = await transactionMutex.LockAsync(cancellationToken).ConfigureAwait(false);
            var inner = innerFunc();
            await foreach (var item in inner.WithCancellation(cancellationToken).ConfigureAwait(false)) yield return item;
        }
    }
}