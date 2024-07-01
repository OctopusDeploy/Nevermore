using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Diagnositcs;

namespace Nevermore.Advanced
{
    /// <summary>
    ///     This class provides a best-effort deadlock detection mechanism. It will identify re-entrant calls from the same
    ///     task (if there is a task) or the same thread (if there is no task). While it does not _guarantee_ deadlock
    ///     detection,
    ///     it does provide a pretty good guarantee that _if_ a DeadlockException is thrown then there was almost certainly
    ///     going to be a deadlock. In other words: very few false positives; probably some false negatives; better than
    ///     nothing.
    /// </summary>
    public class DeadlockAwareLock : IDisposable
    {
        static readonly ILog Log = LogProvider.For<DeadlockAwareLock>();
        
        readonly SemaphoreSlim semaphore = new(1, 1);
        
        int? taskWhichHasAcquiredLock;
        int? threadWhichHasAcquiredLock;

        readonly bool logOnConcurrentExecution;

        public DeadlockAwareLock(bool logOnConcurrentExecution)
        {
            this.logOnConcurrentExecution = logOnConcurrentExecution;
        }

        public void Wait()
        {
            AssertNoDeadlock();

            if (logOnConcurrentExecution && semaphore.CurrentCount == 0)
            {
                Log.Warn("Concurrent query execution detected while waiting for lock");
            }
                
            semaphore.Wait();
            RecordLockAcquisition();
        }

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            AssertNoDeadlock();
            
            if (logOnConcurrentExecution && semaphore.CurrentCount == 0)
            {
                Log.Warn("Concurrent query execution detected while waiting for lock");
            }
            
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            RecordLockAcquisition();
        }

        public void Release()
        {
            threadWhichHasAcquiredLock = null;
            taskWhichHasAcquiredLock = null;
            semaphore.Release();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AssertNoDeadlock()
        {
            if (taskWhichHasAcquiredLock is not null)
            {
                // If we have a task then we can rely on the task ID. It's not guaranteed (one task can still spawn another) but it's better than nothing.
                // If it's a different task which has acquired the lock then there's at least _some_ hope that that task might complete without requiring
                // this task to complete. If this task has the lock and is attempting to acquire it again then there's no way out - deadlock.
                if (taskWhichHasAcquiredLock == Task.CurrentId)
                    throw new DeadlockException("This task context has already acquired this lock and has attempted to do so again.");
            }
            else
            {
                // If we have no task then our best guess is that we're using sync-only code, which means that the thread ID _should_ be
                // a good indicator of the call context.
                if (threadWhichHasAcquiredLock is not null)
                    // If this thread has already acquired a lock and it's trying to do so again then
                    // it's very unlikely that the first lock will be released, hence deadlock.
                    if (threadWhichHasAcquiredLock == Thread.CurrentThread.ManagedThreadId)
                        throw new DeadlockException("This thread has already acquired this lock and has attempted to do so again.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RecordLockAcquisition()
        {
            threadWhichHasAcquiredLock = Thread.CurrentThread.ManagedThreadId;
            taskWhichHasAcquiredLock = Task.CurrentId;
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }
    }
}