namespace NServiceBus.Timeout.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Hosting.Windows.Persistence;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Embedded;

    [TestFixture, Ignore]
    public class When_receiving_timeouts_with_raven : When_receiving_timeouts
    {
        public override IPersistTimeouts TimeoutPersister
        {
            get
            {
                IDocumentStore store = new EmbeddableDocumentStore { RunInMemory = true };
                //IDocumentStore store = new DocumentStore { Url = "http://localhost:8080", DefaultDatabase = "TempTest" };
                store.Conventions.DefaultQueryingConsistency = ConsistencyOptions.MonotonicRead;
                store.Conventions.MaxNumberOfRequestsPerSession = 10;
                store.Initialize();

                return new RavenTimeoutPersistence(store);
            }
        }
    }

    [TestFixture, Ignore]
    public class When_receiving_timeouts_with_inmemory : When_receiving_timeouts
    {
        public override IPersistTimeouts TimeoutPersister
        {
            get
            {
                return new InMemoryTimeoutPersistence();
            }
        }
    }

    public abstract class When_receiving_timeouts
    {
        private IManageTimeouts manager;
        private FakeMessageSender messageSender;
        readonly Random rand = new Random();
        private int expected;

        public abstract IPersistTimeouts TimeoutPersister { get; }

        [SetUp]
        public void Setup()
        {
            Address.InitializeLocalAddress("MyEndpoint");

            Configure.GetEndpointNameAction = () => "MyEndpoint";

            messageSender = new FakeMessageSender();
            manager = new DefaultTimeoutManager
                {
                    TimeoutsPersister = TimeoutPersister,
                    MessageSender = messageSender,
                };
            
        }

        [TearDown]
        public void Cleanup()
        {
        }

        [Test]
        public void Should_send_all_timeout_messages_that_expired()
        {
            expected = 500;

            Parallel.For(0, 500, i => manager.PushTimeout(CreateData(DateTime.UtcNow.AddSeconds(-5))));

            WaitForMessagesThenAssert(30);
        }

        [Test]
        public void Should_pickup_future_timeout_messages_and_send_when_expired()
        {
            expected = 1;

            manager.PushTimeout(CreateData(DateTime.UtcNow.AddSeconds(2)));

            WaitForMessagesThenAssert(5);
        }

        [Test]
        public void Should_send_more_timeout_messages_after_completed_a_batch()
        {
            expected = 250;
            Parallel.For(0, 250, i => manager.PushTimeout(CreateData(DateTime.UtcNow.AddSeconds(1))));

            WaitForMessagesThenAssert(10);

            messageSender.MessagesSent = 0;
            expected = 300;

            Parallel.For(0, 300, i => manager.PushTimeout(CreateData(DateTime.UtcNow.AddSeconds(3))));

            WaitForMessagesThenAssert(10);
        }

        [Test]
        public void Should_pickup_new_timeout_messages_as_they_arrive_and_send_all()
        {
            expected = 1000;

            Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    Parallel.For(0, 500, i => manager.PushTimeout(CreateData(DateTime.UtcNow.AddSeconds(rand.Next(0, 5)))));
                });
            Parallel.For(0, 500, i => manager.PushTimeout(CreateData(DateTime.UtcNow.AddSeconds(1))));

            WaitForMessagesThenAssert(30);
        }

        [Test]
        public void Should_pickup_new_timeout_messages_even_if_they_due_now_and_send_all()
        {
            expected = 250;
            Parallel.For(0, 250, i => manager.PushTimeout(CreateData(DateTime.UtcNow.AddSeconds(1))));

            WaitForMessagesThenAssert(20);

            messageSender.MessagesSent = 0;
            expected = 400;

            Task.Factory.StartNew(() =>
            {
                Parallel.For(0, 100, i => manager.PushTimeout(CreateData(DateTime.UtcNow.AddSeconds(rand.Next(0, 40)))));
            });

            Parallel.For(0, 300, i => manager.PushTimeout(CreateData(DateTime.UtcNow.AddSeconds(3))));

            WaitForMessagesThenAssert(90);
        }

        private TimeoutData CreateData(DateTime time)
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
            DateTime maxTime = DateTime.Now.AddSeconds(maxSecondsToWait);

            while (messageSender.MessagesSent < expected && DateTime.Now < maxTime)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            Assert.AreEqual(expected, messageSender.MessagesSent);
        }
    }
}