using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Advanced
{
    public static class DeadlockAwareLockExtensions
    {
        public static IDisposable Lock(this LockWithLoggingConcurrencyHandler lockWithLoggingConcurrencyHandler)
        {
            lockWithLoggingConcurrencyHandler.Wait();
            return new Disposable(lockWithLoggingConcurrencyHandler.Release);
        }
        
        public static async Task<IDisposable> LockAsync(
            this LockWithLoggingConcurrencyHandler lockWithLoggingConcurrencyHandler,
            CancellationToken cancellationToken)
        {
            await lockWithLoggingConcurrencyHandler.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new Disposable(lockWithLoggingConcurrencyHandler.Release);
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