using System;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Advanced;
using Nito.AsyncEx;
using NUnit.Framework;

namespace Nevermore.Tests
{
    public class DeadlockAwareLockFixture
    {
        CancellationToken cancellationToken;
        CancellationTokenSource cts;

        [SetUp]
        public void SetUp()
        {
            cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            cancellationToken = cts.Token;
        }

        [TearDown]
        public void TearDown()
        {
            cts?.Dispose();
        }

        [Test]
        public void MultipleCallsToWait_ShouldThrow()
        {
            using var deadlockAwareLock = new DeadlockAwareLock();

            deadlockAwareLock.Wait();

            // The second call should immediately throw rather than waiting forever.
            Assert.Throws<DeadlockException>(() => deadlockAwareLock.Wait());
        }

        [Test]
        public void MultipleCallsToWaitWithinATask_ShouldThrow()
        {
            Task.Run(() =>
                {
                    // ReSharper disable AccessToDisposedClosure
                    using var deadlockAwareLock = new DeadlockAwareLock();

                    deadlockAwareLock.Wait();

                    // The second call should immediately throw rather than waiting forever.
                    Assert.Throws<DeadlockException>(() => deadlockAwareLock.Wait());

                    // ReSharper restore AccessToDisposedClosure
                }, cancellationToken)
                .Wait(cancellationToken);
        }

        [Test]
        public void AcquiringThenReleasingThenAcquiring_ShouldNotThrow()
        {
            using var deadlockAwareLock = new DeadlockAwareLock();

            deadlockAwareLock.Wait();
            deadlockAwareLock.Release();
            deadlockAwareLock.Wait();
        }

        [Test]
        public void AcquiringThenReleasingThenAcquiringInATask_ShouldNotThrow()
        {
            Task.Run(() =>
                {
                    // ReSharper disable AccessToDisposedClosure
                    using var deadlockAwareLock = new DeadlockAwareLock();

                    deadlockAwareLock.Wait();
                    deadlockAwareLock.Release();
                    deadlockAwareLock.Wait();

                    // ReSharper restore AccessToDisposedClosure
                }, cancellationToken)
                .Wait(cancellationToken);
        }

        [Test]
        public async Task MultipleCallsToWaitAsync_ShouldThrow()
        {
            using var deadlockAwareLock = new DeadlockAwareLock();

            await deadlockAwareLock.WaitAsync(cancellationToken);

            // The second call should immediately throw rather than waiting forever.
            Assert.ThrowsAsync<DeadlockException>(async () => await deadlockAwareLock.WaitAsync(cancellationToken));
        }

        [Test]
        public async Task AcquiringAsyncThenReleasingThenAcquiringAsync_ShouldNotThrow()
        {
            using var deadlockAwareLock = new DeadlockAwareLock();

            await deadlockAwareLock.WaitAsync(cancellationToken);
            deadlockAwareLock.Release();
            await deadlockAwareLock.WaitAsync(cancellationToken);
        }

        [Test]
        public async Task MultipleTasksContending_ShouldNotThrow()
        {
            // ReSharper disable AccessToDisposedClosure
            using var deadlockAwareLock = new DeadlockAwareLock();

            // Loop so that we increase the probability that two different tasks are scheduled onto
            // the same worker thread. This helps us guarantee that we're not accidentally relying
            // on thread IDs or thread locals anywhere.
            for (var i = 0; i < 1000; i++)
            {
                var task0 = Task.Run(async () =>
                {
                    await deadlockAwareLock.WaitAsync(cancellationToken);
                    await Task.Yield();
                    deadlockAwareLock.Release();
                }, cancellationToken);

                var task1 = Task.Run(async () =>
                {
                    await deadlockAwareLock.WaitAsync(cancellationToken);
                    await Task.Yield();
                    deadlockAwareLock.Release();
                }, cancellationToken);

                await Task.WhenAll(task0, task1);
            }

            // ReSharper restore AccessToDisposedClosure
        }

        [Test]
        public void UsingSyncExtensionMethods_AndReleasingLocksCorrectly_ShouldNotThrow()
        {
            using var deadlockAwareLock = new DeadlockAwareLock();

            using (var _ = deadlockAwareLock.Lock())
            {
            }

            using (var _ = deadlockAwareLock.Lock())
            {
            }
        }

        [Test]
        public async Task UsingAsyncExtensionMethods_AndReleasingLocksCorrectly_ShouldNotThrow()
        {
            using var deadlockAwareLock = new DeadlockAwareLock();

            using (var _ = await deadlockAwareLock.LockAsync(cancellationToken))
            {
            }

            using (var _ = await deadlockAwareLock.LockAsync(cancellationToken))
            {
            }
        }
    }
}