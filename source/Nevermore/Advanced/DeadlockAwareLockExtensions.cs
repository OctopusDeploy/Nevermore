using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Advanced
{
    public static class DeadlockAwareLockExtensions
    {
        public static IDisposable Lock(this DeadlockAwareLock deadlockAwareLock)
        {
            deadlockAwareLock.Wait();
            return new Disposable(deadlockAwareLock.Release);
        }
        
        public static async Task<IDisposable> LockAsync(
            this DeadlockAwareLock deadlockAwareLock,
            CancellationToken cancellationToken)
        {
            await deadlockAwareLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new Disposable(deadlockAwareLock.Release);
        }
        
        class Disposable : IDisposable
        {
            readonly Action callback;
        
            public Disposable(Action callback)
            {
                this.callback = callback;
            }
        
            public void Dispose()
            {
                callback();
            }
        }
    }
}