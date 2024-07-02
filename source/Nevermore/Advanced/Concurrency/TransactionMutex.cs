using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.Disposables;

namespace Nevermore.Advanced.Concurrency
{
    public class TransactionMutex : IDisposable
    {
        readonly SemaphoreSlim _loggingSemaphore = new(1, 1);
        readonly SemaphoreSlim _lockingSemaphore = new(1, 1);

        readonly ConcurrencyMode _concurrencyMode;

        bool ShouldLog => _concurrencyMode is ConcurrencyMode.LockAndWarn or ConcurrencyMode.WarnOnly;
        bool ShouldLock => _concurrencyMode is ConcurrencyMode.LockOnly or ConcurrencyMode.LockAndWarn;

        readonly Action _logCallback;

        public TransactionMutex(ConcurrencyMode concurrencyMode, Action logCallback)
        {
            _concurrencyMode = concurrencyMode;
            _logCallback = logCallback;
        }

        public IDisposable Lock()
        {
            List<IDisposable> disposables = new();
            if (ShouldLog)
            {
                if (_loggingSemaphore.Wait(TimeSpan.Zero))
                {
                   // Got the lock, no need to log 
                    disposables.Add(new Disposable(() => _loggingSemaphore.Release()));        
                }
                else
                {
                    // Didn't get the lock, something else has it, need to log
                    _logCallback.Invoke();
                }
            }

            if (ShouldLock)
            {
                disposables.Add(_lockingSemaphore.Lock());
            }
            
            return CollectionDisposable.Create(disposables);
        }

        public async Task<IDisposable> LockAsync(CancellationToken cancellationToken)
        {
            List<IDisposable> disposables = new();
            if (ShouldLog)
            {
                if (await _loggingSemaphore.WaitAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false))
                {
                    // Got the lock, no need to log 
                    disposables.Add(new Disposable(() => _loggingSemaphore.Release()));        
                }
                else
                {
                    // Didn't get the lock, something else has it, need to log
                    _logCallback.Invoke();
                }
            }

            if (ShouldLock)
            {
                disposables.Add(await _lockingSemaphore.LockAsync(cancellationToken).ConfigureAwait(false));
            }
            
            return CollectionDisposable.Create(disposables);
        }

        public void Dispose()
        {
            _loggingSemaphore?.Dispose();
            _lockingSemaphore?.Dispose();
        }
    }
}