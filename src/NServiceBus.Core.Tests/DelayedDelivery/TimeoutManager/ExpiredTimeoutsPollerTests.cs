namespace NServiceBus.Core.Tests.Timeout.TimeoutManager
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;
    using Transport;

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
            await poller.SpinOnce(CancellationToken.None);
            CollectionAssert.IsEmpty(dispatcher.DispatchedMessages);
        }

        [Test]
        public async Task Returns_to_normal_poll_cycle_after_dispatching_a_pushed_timeout()
        {
            await poller.SpinOnce(CancellationToken.None);
            var nextRetrieval = poller.NextRetrieval;

            RegisterNewTimeout(nextRetrieval - HalfOfDefaultInMemoryPersisterSleep);

            currentTime = poller.NextRetrieval;

            await poller.SpinOnce(CancellationToken.None);

            Assert.AreEqual(1, dispatcher.DispatchedMessages.Count);
            Assert.AreEqual(currentTime + InMemoryTimeoutPersister.EmptyResultsNextTimeToRunQuerySpan, poller.NextRetrieval);
        }

        [Test]
        public async Task Returns_to_normal_poll_cycle_after_dispatching_a_non_pushed_timeout()
        {
            var nextRetrieval = poller.NextRetrieval;
            var timeout1 = nextRetrieval.Subtract(HalfOfDefaultInMemoryPersisterSleep);
            // ReSharper disable once PossibleLossOfFraction
            var timeout2 = timeout1.Add(TimeSpan.FromMilliseconds(HalfOfDefaultInMemoryPersisterSleep.Milliseconds / 2));

            RegisterNewTimeout(timeout1);
            RegisterNewTimeout(timeout2, false);

            currentTime = timeout2;
            await poller.SpinOnce(CancellationToken.None);

            Assert.AreEqual(2, dispatcher.DispatchedMessages.Count);
            Assert.AreEqual(currentTime + InMemoryTimeoutPersister.EmptyResultsNextTimeToRunQuerySpan, poller.NextRetrieval);
        }

        [Test]
        public async Task Poll_with_same_start_slice_from_last_failed_dispatch()
        {
            var failingDispatcher = new FailableDispatcher();
            poller = new ExpiredTimeoutsPoller(timeouts, failingDispatcher, "test", breaker, () => currentTime);

            RegisterNewTimeout(currentTime.Subtract(TimeSpan.FromMinutes(5)));

            var dispatchCalls = 0;
            var unicastTransportOperations = new List<UnicastTransportOperation>();
            failingDispatcher.DispatcherAction = m =>
            {
                if (++dispatchCalls == 1)
                {
                    // fail first dispatch
                    throw new Exception("transport error");
                }

                // succeed second dispatch
                unicastTransportOperations = m.UnicastTransportOperations;
                return TaskEx.CompletedTask;
            };

            try
            {
                await poller.SpinOnce(CancellationToken.None);
            }
            catch (Exception)
            {
                // ignore. An exception will cause another polling attempt.
            }

            await poller.SpinOnce(CancellationToken.None);

            Assert.AreEqual(1, unicastTransportOperations.Count);
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
        TimeSpan HalfOfDefaultInMemoryPersisterSleep = TimeSpan.FromMilliseconds(InMemoryTimeoutPersister.EmptyResultsNextTimeToRunQuerySpan.TotalMilliseconds / 2);
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

        class FailableDispatcher : IDispatchMessages
        {
            public Func<TransportOperations, Task> DispatcherAction { get; set; }

            public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
            {
                return DispatcherAction(outgoingMessages);
            }
        }
    }
}