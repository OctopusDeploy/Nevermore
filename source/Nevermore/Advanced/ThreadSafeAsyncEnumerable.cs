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
        readonly SemaphoreSlim semaphore;

        public ThreadSafeAsyncEnumerable(IAsyncEnumerable<T> inner, SemaphoreSlim semaphore) : this(() => inner, semaphore)
        {
        }

        public ThreadSafeAsyncEnumerable(Func<IAsyncEnumerable<T>> innerFunc, SemaphoreSlim semaphore)
        {
            this.innerFunc = innerFunc;
            this.semaphore = semaphore;
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
        {
            using var mutex = await semaphore.LockAsync(cancellationToken);
            var inner = innerFunc();
            await foreach (var item in inner.WithCancellation(cancellationToken)) yield return item;
        }
    }
}