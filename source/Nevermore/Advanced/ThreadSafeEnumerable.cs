using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Nevermore.Advanced
{
    internal class ThreadSafeEnumerable<T> : IEnumerable<T>
    {
        readonly Func<IEnumerable<T>> innerFunc;
        readonly DeadlockAwareLock deadlockAwareLock;

        public ThreadSafeEnumerable(IEnumerable<T> inner, DeadlockAwareLock deadlockAwareLock) : this(() => inner, deadlockAwareLock)
        {
        }

        public ThreadSafeEnumerable(Func<IEnumerable<T>> innerFunc, DeadlockAwareLock deadlockAwareLock)
        {
            this.innerFunc = innerFunc;
            this.deadlockAwareLock = deadlockAwareLock;
        }

        public IEnumerator<T> GetEnumerator()
        {
            deadlockAwareLock.Wait();
            var inner = innerFunc();
            return new ThreadSafeEnumerator(inner.GetEnumerator(), () => deadlockAwareLock.Release());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal class ThreadSafeEnumerator : IEnumerator<T>
        {
            readonly IEnumerator<T> inner;
            readonly Action onDisposed;

            public ThreadSafeEnumerator(IEnumerator<T> inner, Action onDisposed)
            {
                this.inner = inner;
                this.onDisposed = onDisposed;
            }

            public bool MoveNext()
            {
                var moveNext = inner.MoveNext();
                return moveNext;
            }

            public void Reset()
            {
                inner.Reset();
            }

            public T Current => inner.Current;

            object IEnumerator.Current => ((IEnumerator) inner).Current;

            public void Dispose()
            {
                inner.Dispose();
                onDisposed();
            }
        }
    }
}