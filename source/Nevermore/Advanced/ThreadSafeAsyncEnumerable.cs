using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Advanced
{
    public class ThreadSafeAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        readonly Func<IAsyncEnumerable<T>> innerFunc;
        readonly DeadlockAwareLock deadlockAwareLock;

        public ThreadSafeAsyncEnumerable(IAsyncEnumerable<T> inner, DeadlockAwareLock deadlockAwareLock) : this(() => inner, deadlockAwareLock)
        {
        }

        public ThreadSafeAsyncEnumerable(Func<IAsyncEnumerable<T>> innerFunc, DeadlockAwareLock deadlockAwareLock)
        {
            this.innerFunc = innerFunc;
            this.deadlockAwareLock = deadlockAwareLock;
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
        {
            await deadlockAwareLock.WaitAsync(cancellationToken);
            try
            {
                var inner = innerFunc();
                await foreach (var item in inner.WithCancellation(cancellationToken)) yield return item;
            }
            finally
            {
                deadlockAwareLock.Release();
            }
        }
    }
}