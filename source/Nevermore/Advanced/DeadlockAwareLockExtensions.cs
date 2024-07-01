using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.Disposables;

namespace Nevermore.Advanced
{
    public static class DeadlockAwareLockExtensions
    {
        public static IDisposable Lock(this DeadlockAwareLock deadlockAwareLock)
        {
            deadlockAwareLock.Wait();
            return new Disposable(deadlockAwareLock.Release);
        }
        
        static async Task<IDisposable> DoLockAsync(
            this DeadlockAwareLock deadlockAwareLock,
            CancellationToken cancellationToken)
        {
            await deadlockAwareLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            return Disposable.Create(deadlockAwareLock.Release);
        }
        
        public static AwaitableDisposable<IDisposable> LockAsync(
            this DeadlockAwareLock deadlockAwareLock,
            CancellationToken cancellationToken)
        {
            return new AwaitableDisposable<IDisposable>(DoLockAsync(deadlockAwareLock, cancellationToken));
        }
    }
}