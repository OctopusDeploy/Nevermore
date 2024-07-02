using System;
using System.Collections;
using System.Collections.Generic;
using Nevermore.Advanced.Concurrency;

namespace Nevermore.Advanced
{
    internal class ThreadSafeEnumerable<T> : IEnumerable<T>
    {
        readonly Func<IEnumerable<T>> innerFunc;
        readonly ITransactionConcurrencyHandler transactionConcurrencyHandler;

        public ThreadSafeEnumerable(IEnumerable<T> inner, ITransactionConcurrencyHandler transactionConcurrencyHandler)
            : this(() => inner, transactionConcurrencyHandler)
        {
        }

        public ThreadSafeEnumerable(Func<IEnumerable<T>> innerFunc, ITransactionConcurrencyHandler transactionConcurrencyHandler)
        {
            this.innerFunc = innerFunc;
            this.transactionConcurrencyHandler = transactionConcurrencyHandler;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var disposable = transactionConcurrencyHandler.Lock();
            var inner = innerFunc();
            return new ThreadSafeEnumerator(inner.GetEnumerator(), () => disposable.Dispose());
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