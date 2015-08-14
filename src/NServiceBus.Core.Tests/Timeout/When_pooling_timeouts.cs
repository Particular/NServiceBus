namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.InMemory.TimeoutPersister;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Timeout.Hosting.Windows;
    using NUnit.Framework;

    [TestFixture]
    [Explicit]
    class When_pooling_timeouts_with_inMemory
    {
        DefaultTimeoutManager manager;
        FakeMessageSender messageSender;
        Random rand = new Random();
        int expected;

        InMemoryTimeoutPersister persister;
        TimeoutPersisterReceiver receiver;
        TimeoutPersistenceOptions options;

        [SetUp]
        public void Setup()
        {
            options = new TimeoutPersistenceOptions(new ContextBag());
            persister = new InMemoryTimeoutPersister();
            messageSender = new FakeMessageSender();

            manager = new DefaultTimeoutManager(persister, messageSender);

            receiver = new TimeoutPersisterReceiver(persister, messageSender, manager, null)
            {
                SecondsToSleepBetweenPolls = 1,
            };
        }

        [Test]
        public void Should_retrieve_all_timeout_messages_that_expired()
        {
            expected = 50;

            for (var i = 0; i < expected; i++)
                persister.Add(CreateData(DateTime.UtcNow.AddSeconds(-5)), options);

            StartAndStopReceiver();

            WaitForMessagesThenAssert(5);
        }

        [Test]
        public void Should_pickup_future_timeout_messages_and_send_when_expired()
        {
            expected = 1;

            manager.PushTimeout(CreateData(DateTime.UtcNow.AddSeconds(2)), options);

            StartAndStopReceiver(5);

            WaitForMessagesThenAssert(5);
        }

        [Test]
        public void Should_send_more_timeout_messages_after_completed_a_batch()
        {
            receiver.Start();

            expected = 10;
            Push(expected, DateTime.UtcNow.AddSeconds(1));

            Thread.Sleep(TimeSpan.FromSeconds(5));

            WaitForMessagesThenAssert(10);

            messageSender.MessagesSent = 0;
            expected = 30;

            Push(expected, DateTime.UtcNow.AddSeconds(3));

            Thread.Sleep(TimeSpan.FromSeconds(8));
            receiver.Stop();

            WaitForMessagesThenAssert(10);
        }

        [Test]
        public void Should_pickup_new_timeout_messages_as_they_arrive_and_send_all()
        {
            expected = 100;

            receiver.Start();

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                Push(50, DateTime.UtcNow.AddSeconds(rand.Next(0, 5)));
            });

            Push(50, DateTime.UtcNow.AddSeconds(1));

            Thread.Sleep(TimeSpan.FromSeconds(20));

            receiver.Stop();

            WaitForMessagesThenAssert(10);
        }

        [Test]
        public void Should_pickup_new_timeout_messages_even_if_they_due_now_and_send_all()
        {
            expected = 25;

            receiver.Start();

            Push(25, DateTime.UtcNow.AddSeconds(1));

            Thread.Sleep(TimeSpan.FromSeconds(5));

            WaitForMessagesThenAssert(5);

            messageSender.MessagesSent = 0;
            expected = 40;

            Task.Factory.StartNew(() => Push(10, DateTime.UtcNow.AddSeconds(rand.Next(0, 30))));

            Push(30, DateTime.UtcNow.AddSeconds(3));

            Thread.Sleep(TimeSpan.FromSeconds(40));

            receiver.Stop();

            WaitForMessagesThenAssert(10);
        }

        private void Push(int total, DateTime time)
        {
            for (var i = 0; i < total; i++)
                manager.PushTimeout(CreateData(time), options);
        }

        private void StartAndStopReceiver(int secondsToWaitBeforeCallingStop = 1)
        {
            receiver.Start();
            Thread.Sleep(TimeSpan.FromSeconds(secondsToWaitBeforeCallingStop));
            receiver.Stop();
        }

        private static TimeoutData CreateData(DateTime time)
        {
            return new TimeoutData
            {
                OwningTimeoutManager = "MyEndpoint",
                Time = time,
                Headers = new Dictionary<string, string>(),
            };
        }

        private void WaitForMessagesThenAssert(int maxSecondsToWait)
        {
            var maxTime = DateTime.Now.AddSeconds(maxSecondsToWait);

            while (messageSender.MessagesSent < expected && DateTime.Now < maxTime)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            Assert.AreEqual(expected, messageSender.MessagesSent);
        }
    }
}