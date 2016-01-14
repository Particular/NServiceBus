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
            timeouts = new InMemoryTimeoutPersister();
            poller = new ExpiredTimeoutsPoller(timeouts, dispatcher, "test", breaker, TimeSpan.Zero, TimeSpan.Zero);
        }

        [TearDown]
        public void TearDown()
        {
            poller.Dispose();
        }

        [Test]
        public async void Sends_no_messages_when_no_timeouts_registered()
        {
            await poller.SpinOnce();
            CollectionAssert.IsEmpty(dispatcher.DispatchedMessages);
        }

        [Test]
        public async void Updates_next_retrieval_time_when_timeout_registered_in_the_middle_and_dispatches_the_message()
        {
            await poller.SpinOnce();

            var nextRetrieval = poller.NextRetrieval;
            var newTimeout = nextRetrieval.Subtract(HalfOfDefaultInMemoryPersisterSleep);

            RegisterNewTimeout(newTimeout);
            Assert.AreEqual(nextRetrieval, poller.NextRetrieval);

            await poller.SpinOnce();

            Assert.AreEqual(nextRetrieval, poller.NextRetrieval);
            Assert.AreEqual(1, dispatcher.DispatchedMessages.Count);
        }

        [Test]
        public async void Keeps_next_retrieval_equal_to_the_registered_timeout_even_when_persister_returns_more_timeouts_and_dispatches_both_timeouts()
        {
            var nextRetrieval = poller.NextRetrieval;
            var timeout1 = nextRetrieval.Subtract(HalfOfDefaultInMemoryPersisterSleep);
            // ReSharper disable once PossibleLossOfFraction
            var timeout2 = timeout1.Add(TimeSpan.FromMilliseconds(HalfOfDefaultInMemoryPersisterSleep.Milliseconds/2));

            RegisterNewTimeout(timeout1);
            RegisterNewTimeout(timeout2, false);

            await poller.SpinOnce();

            Assert.AreEqual(timeout1, poller.NextRetrieval);
            Assert.AreEqual(2, dispatcher.DispatchedMessages.Count);
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
        // ReSharper disable once PossibleLossOfFraction
        TimeSpan HalfOfDefaultInMemoryPersisterSleep = TimeSpan.FromMilliseconds(InMemoryTimeoutPersister.EmptyResultsNextTimeToRunQuerySpan.Milliseconds/2);
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