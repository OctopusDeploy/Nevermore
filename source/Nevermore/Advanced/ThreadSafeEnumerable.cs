using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Nevermore.Advanced
{
    internal class ThreadSafeEnumerable<T> : IEnumerable<T>
    {
        readonly Func<IEnumerable<T>> innerFunc;
        readonly LockWithLoggingConcurrencyHandler _lockWithLoggingConcurrencyHandler;

        public ThreadSafeEnumerable(IEnumerable<T> inner, LockWithLoggingConcurrencyHandler lockWithLoggingConcurrencyHandler) : this(() => inner, lockWithLoggingConcurrencyHandler)
        {
        }

        public ThreadSafeEnumerable(Func<IEnumerable<T>> innerFunc, LockWithLoggingConcurrencyHandler lockWithLoggingConcurrencyHandler)
        {
            this.innerFunc = innerFunc;
            this._lockWithLoggingConcurrencyHandler = lockWithLoggingConcurrencyHandler;
        }

        public IEnumerator<T> GetEnumerator()
        {
            _lockWithLoggingConcurrencyHandler.Wait();
            var inner = innerFunc();
            return new ThreadSafeEnumerator(inner.GetEnumerator(), () => _lockWithLoggingConcurrencyHandler.Release());
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