namespace NServiceBus.Core.Tests.Timeout.TimeoutManager
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    public class ExpiredTimeoutsPollerTests
    {
        [SetUp]
        public void SetUp()
        {
            breaker = new FakeBreaker();
            dispatcher = new RecordingFakeDispatcher();
            timeouts = new InMemoryTimeoutPersister(() => currentTime);
            poller = new ExpiredTimeoutsPoller(timeouts, dispatcher, "test", breaker, () => currentTime);
        }

        [TearDown]
        public void TearDown()
        {
            poller.Dispose();
        }

        [Test]
        public async Task Sends_no_messages_when_no_timeouts_registered()
        {
            await poller.SpinOnce();
            CollectionAssert.IsEmpty(dispatcher.DispatchedMessages);
        }

        [Test]
        public async Task Returns_to_normal_poll_cycle_after_dispatching_a_pushed_timeout()
        {
            await poller.SpinOnce();
            var nextRetrieval = poller.NextRetrieval;

            RegisterNewTimeout(nextRetrieval - HalfOfDefaultInMemoryPersisterSleep);

            currentTime = poller.NextRetrieval;

            await poller.SpinOnce();

            Assert.AreEqual(1, dispatcher.DispatchedMessages.Count);
            Assert.AreEqual(currentTime + InMemoryTimeoutPersister.EmptyResultsNextTimeToRunQuerySpan, poller.NextRetrieval);
        }

        [Test]
        public async Task Returns_to_normal_poll_cycle_after_dispatching_a_non_pushed_timeout()
        {
            var nextRetrieval = poller.NextRetrieval;
            var timeout1 = nextRetrieval.Subtract(HalfOfDefaultInMemoryPersisterSleep);
            // ReSharper disable once PossibleLossOfFraction
            var timeout2 = timeout1.Add(TimeSpan.FromMilliseconds(HalfOfDefaultInMemoryPersisterSleep.Milliseconds/2));

            RegisterNewTimeout(timeout1);
            RegisterNewTimeout(timeout2, false);

            currentTime = timeout2;
            await poller.SpinOnce();

            Assert.AreEqual(2, dispatcher.DispatchedMessages.Count);
            Assert.AreEqual(currentTime + InMemoryTimeoutPersister.EmptyResultsNextTimeToRunQuerySpan, poller.NextRetrieval);
        }

        void RegisterNewTimeout(DateTime newTimeout, bool withNotification = true)
        {
            timeouts.Add(new TimeoutData
            {
                Time = newTimeout
            }, null);
            if (withNotification)
            {
                poller.NewTimeoutRegistered(newTimeout);
            }
        }

        FakeBreaker breaker;
        RecordingFakeDispatcher dispatcher;
        DateTime currentTime = DateTime.UtcNow;
        // ReSharper disable once PossibleLossOfFraction
        TimeSpan HalfOfDefaultInMemoryPersisterSleep = TimeSpan.FromMilliseconds(InMemoryTimeoutPersister.EmptyResultsNextTimeToRunQuerySpan.TotalMilliseconds/2);
        ExpiredTimeoutsPoller poller;
        InMemoryTimeoutPersister timeouts;

        class FakeBreaker : ICircuitBreaker
        {
            public void Success()
            {
            }

            public Task Failure(Exception exception)
            {
                return TaskEx.CompletedTask;
            }
        }
    }
}