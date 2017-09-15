namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.InMemory.TimeoutPersister;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Timeout.Hosting.Windows;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;
    using NUnit.Framework;

    [TestFixture]
    public class TimeoutMessageReceiverTests
    {
        [SetUp]
        public void SetUp()
        {
            startSlice = DateTime.UtcNow.AddYears(-10);

            timeouts = new InMemoryTimeoutPersister
            {
                CurrentTime = () => currentTime
            };

            sender = new TestableMessageSender();

            receiver = new TimeoutPersisterReceiver
            {
                CurrentTime = () => currentTime,
                MessageSender = sender,
                TimeoutsPersister = timeouts,
                DispatcherAddress = new Address("test", String.Empty),
                CriticalError = new CriticalError((msg, ex) => { }, new Configure(new SettingsHolder(), new FuncBuilder(), new List<Action<IConfigureComponents>>(), new PipelineSettings(null)))
            };
        }

        [Test]
        public void Sends_no_messages_when_no_timeouts_registered()
        {
            receiver.SpinOnce(startSlice);
            Assert.AreEqual(0, sender.MessagesSent, "Messages Sent");
        }

        [Test]
        public void Returns_to_normal_poll_cycle_after_dispatching_a_pushed_timeout()
        {
            receiver.SpinOnce(startSlice);

            var nextRetrieval = receiver.NextRetrieval;

            RegisterNewTimeout(nextRetrieval - HalfOfDefaultInMemoryPersisterSleep);

            currentTime = receiver.NextRetrieval;

            receiver.SpinOnce(startSlice);

            Assert.AreEqual(1, sender.MessagesSent, "Messages Sent");
            Assert.AreEqual((currentTime + EmptyResultsNextTimeToRunQuerySpan), receiver.NextRetrieval, "Next Retrieval");
        }

        [Test]
        public void Returns_to_normal_poll_cycle_after_dispatching_a_non_pushed_timeout()
        {
            var nextRetrieval = receiver.NextRetrieval;
            var timeout1 = nextRetrieval.Subtract(HalfOfDefaultInMemoryPersisterSleep);

            // ReSharper disable once PossibleLossOfFraction
            var timeout2 = timeout1.Add(TimeSpan.FromMilliseconds(HalfOfDefaultInMemoryPersisterSleep.Milliseconds / 2));

            RegisterNewTimeout(timeout1);
            RegisterNewTimeout(timeout2, false);

            currentTime = timeout2;
            receiver.SpinOnce(startSlice);

            Assert.AreEqual(2, sender.MessagesSent, "Messages Sent");
            Assert.AreEqual(currentTime + EmptyResultsNextTimeToRunQuerySpan, receiver.NextRetrieval, "Next Retrieval");
        }

        [Test]
        public void Poll_with_same_start_slice_from_last_failed_dispatch()
        {
            RegisterNewTimeout(currentTime.Subtract(TimeSpan.FromMinutes(5)));

            var dispatchCalls = 0;
            var transportOperations = new List<TransportMessage>();

            sender.SendAction = m =>
            {
                if (++dispatchCalls == 1)
                {
                    // fail first dispatch
                    throw new Exception("transport error");
                }

                // succeed second dispatch
                transportOperations.Add(m);
            };

            try
            {
                receiver.SpinOnce(startSlice);
            }
            catch (Exception)
            {
                // ignore. An exception will cause another polling attempt.
            }

            receiver.SpinOnce(startSlice);

            Assert.AreEqual(1, transportOperations.Count);
        }

        void RegisterNewTimeout(DateTime newTimeout, bool withNotification = true)
        {
            var newTimeoutData = new TimeoutData
            {
                Time = newTimeout
            };
            timeouts.Add(newTimeoutData);

            if (withNotification)
            {
                receiver.TimeoutsManagerOnTimeoutPushed(newTimeoutData);
            }
        }

        DateTime currentTime = DateTime.UtcNow;
        private TimeoutPersisterReceiver receiver;
        private TestableMessageSender sender;
        InMemoryTimeoutPersister timeouts;

        static TimeSpan EmptyResultsNextTimeToRunQuerySpan = TimeSpan.FromMinutes(1);
        TimeSpan HalfOfDefaultInMemoryPersisterSleep = TimeSpan.FromMilliseconds(EmptyResultsNextTimeToRunQuerySpan.TotalMilliseconds / 2);
        private DateTime startSlice;

        class TestableMessageSender : ISendMessages
        {
            private volatile int messagesSent;

            public int MessagesSent => messagesSent;

            public Action<TransportMessage> SendAction { private get; set; } = msg => { };

            public void Send(TransportMessage message, SendOptions sendOptions)
            {
                messagesSent++;
                SendAction(message);
            }
        }
    }
}
