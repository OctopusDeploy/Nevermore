using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Advanced
{
    public class DeadlockAwareLock : SemaphoreSlim
    {
        int? taskWhichAsAcquiredLock;

        int? threadWhichHasAcquiredLock;

        public DeadlockAwareLock() : base(1, 1)
        {
        }

        public new void Wait()
        {
            AssertNoDeadlock();
            base.Wait();
            RecordLockAcquisition();
        }

        public new async Task WaitAsync(CancellationToken cancellationToken)
        {
            AssertNoDeadlock();
            await base.WaitAsync(cancellationToken);
            RecordLockAcquisition();
        }

        void AssertNoDeadlock()
        {
            if (taskWhichAsAcquiredLock is not null)
            {
                if (taskWhichAsAcquiredLock == Task.CurrentId) throw new DeadlockException("This task context has already acquired this lock and has attempted to do so again.");
            }
            else
            {
                if (threadWhichHasAcquiredLock is not null)
                    if (threadWhichHasAcquiredLock == Thread.CurrentThread.ManagedThreadId)
                        throw new DeadlockException("This thread has already acquired this lock and has attempted to do so again.");
            }
        }

        void RecordLockAcquisition()
        {
            threadWhichHasAcquiredLock = Thread.CurrentThread.ManagedThreadId;
            taskWhichAsAcquiredLock = Task.CurrentId;
        }

        public new void Release()
        {
            threadWhichHasAcquiredLock = null;
            taskWhichAsAcquiredLock = null;
            base.Release();
        }
    }
}