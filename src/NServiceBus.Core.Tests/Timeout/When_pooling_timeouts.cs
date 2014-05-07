namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Persistence.InMemory.TimeoutPersister;
    using NServiceBus.Persistence.Raven;
    using NServiceBus.Persistence.Raven.TimeoutPersister;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Timeout.Hosting.Windows;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Embedded;

//    [TestFixture]
//    [Explicit]
//    public class When_pooling_timeouts_with_raven : When_pooling_timeouts
//    {
//        IDocumentStore store;
//
//        protected override IPersistTimeouts CreateTimeoutPersister()
//        {
//            store = new EmbeddableDocumentStore
//                    {
//                        RunInMemory = true
//                    };
//            //IDocumentStore store = new DocumentStore { Url = "http://localhost:8080", DefaultDatabase = "TempTest" };
//            store.Conventions.DefaultQueryingConsistency = ConsistencyOptions.QueryYourWrites;
//            store.Conventions.MaxNumberOfRequestsPerSession = 10;
//            store.Initialize();
//
//            return new RavenTimeoutPersistence(new StoreAccessor(store));
//        }
//
//        [TearDown]
//        public void Cleanup()
//        {
//            store.Dispose();
//        }
//    }

    [TestFixture]
   [Explicit]
    public class When_pooling_timeouts_with_inMemory : When_pooling_timeouts
    {
        protected override IPersistTimeouts CreateTimeoutPersister()
        {
            return new InMemoryTimeoutPersistence();
        }
    }

    public abstract class When_pooling_timeouts
    {
        private IManageTimeouts manager;
        private FakeMessageSender messageSender;
        readonly Random rand = new Random();
        private int expected;

        protected IPersistTimeouts persister;
        protected TimeoutPersisterReceiver receiver;

        protected abstract IPersistTimeouts CreateTimeoutPersister();

        [SetUp]
        public void Setup()
        {
            Address.InitializeLocalAddress("MyEndpoint");

            Configure.GetEndpointNameAction = () => "MyEndpoint";

            persister = CreateTimeoutPersister();
            messageSender = new FakeMessageSender();

            manager = new DefaultTimeoutManager
                {
                    TimeoutsPersister = persister,
                    MessageSender = messageSender,
                };

            receiver = new TimeoutPersisterReceiver
                {
                    TimeoutManager = manager,
                    TimeoutsPersister = persister,
                    MessageSender = messageSender,
                    SecondsToSleepBetweenPolls = 1,
                };
        }

        [Test]
        public void Should_retrieve_all_timeout_messages_that_expired()
        {
            expected = 50;

            Enumerable.Range(1, expected).ToList().ForEach(i => persister.Add(CreateData(DateTime.UtcNow.AddSeconds(-5))));
            
            StartAndStopReceiver();

            WaitForMessagesThenAssert(5);
        }

        [Test]
        public void Should_pickup_future_timeout_messages_and_send_when_expired()
        {
            expected = 1;

            manager.PushTimeout(CreateData(DateTime.UtcNow.AddSeconds(2)));

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
            Enumerable.Range(1, total).ToList().ForEach(i => manager.PushTimeout(CreateData(time)));
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
                    OwningTimeoutManager = Configure.EndpointName,
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