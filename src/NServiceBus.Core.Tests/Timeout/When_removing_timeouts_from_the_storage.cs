namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Transactions;
    using NServiceBus.Persistence.InMemory.TimeoutPersister;
    using NServiceBus.Persistence.Raven;
    using NServiceBus.Persistence.Raven.TimeoutPersister;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Embedded;

    [TestFixture]
    public class When_removing_timeouts_from_the_storage_with_raven : When_removing_timeouts_from_the_storage
    {
        private IDocumentStore store;

        protected override IPersistTimeouts CreateTimeoutPersister()
        {
            store = new EmbeddableDocumentStore {RunInMemory = true};
            //store = new DocumentStore { Url = "http://localhost:8080", DefaultDatabase = "TempTest" };
            store.Conventions.DefaultQueryingConsistency = ConsistencyOptions.QueryYourWrites;
            store.Conventions.MaxNumberOfRequestsPerSession = 10;
            store.Initialize();

            return new RavenTimeoutPersistence(new StoreAccessor(store));
        }

        [TearDown]
        public void Cleanup()
        {
            store.Dispose();
        }
    }

    [TestFixture]
    public class When_removing_timeouts_from_the_storage_with_inMemory : When_removing_timeouts_from_the_storage
    {
        protected override IPersistTimeouts CreateTimeoutPersister()
        {
            return new InMemoryTimeoutPersistence();
        }
    }

    public abstract class When_removing_timeouts_from_the_storage
    {
        protected IPersistTimeouts persister;
        protected IPersistTimeoutsV2 persisterV2;

        protected abstract IPersistTimeouts CreateTimeoutPersister();

        [SetUp]
        public void Setup()
        {
            Address.InitializeLocalAddress("MyEndpoint");

            Configure.GetEndpointNameAction = () => "MyEndpoint";

            persister = CreateTimeoutPersister();
            persisterV2 = persister as IPersistTimeoutsV2;
        }

        [Test]
        public void Should_remove_timeouts_by_id()
        {
            var t1 = new TimeoutData {Id = "1", Time = DateTime.UtcNow.AddHours(-1)};
            persister.Add(t1);

            var t2 = new TimeoutData {Id = "2", Time = DateTime.UtcNow.AddHours(-1)};
            persister.Add(t2);

            var timeouts = GetNextChunk();

            foreach (var timeout in timeouts)
            {
                TimeoutData timeoutData;
                persister.TryRemove(timeout.Item1, out timeoutData);
            }

            Assert.AreEqual(0, GetNextChunk().Count);
        }

        [Test]
        public void TryRemove_should_work_with_concurrent_operations()
        {
            var timeoutData = new TimeoutData { Id = "1", Time = DateTime.UtcNow.AddHours(-1) };
            persister.Add(timeoutData);

            var t1EnteredTx = new AutoResetEvent(false);
            var t2EnteredTx = new AutoResetEvent(false);

            bool? t1Remove = null;
            bool? t2Remove = null;
            var t1 = new Thread(() =>
            {
                using (var tx = new TransactionScope())
                {
                    t1EnteredTx.Set();
                    t2EnteredTx.WaitOne();

                    t1Remove = persisterV2.TryRemove(timeoutData.Id);
                    tx.Complete();
                }
            });

            var t2 = new Thread(() =>
            {
                using (var tx = new TransactionScope())
                {
                    t2EnteredTx.Set();
                    t1EnteredTx.WaitOne();

                    t2Remove = persisterV2.TryRemove(timeoutData.Id);
                    tx.Complete();
                }
            });

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();

            Assert.IsTrue(t1Remove.Value || t2Remove.Value);
            Assert.IsFalse(t1Remove.Value && t2Remove.Value);
        }

        protected List<Tuple<string, DateTime>> GetNextChunk()
        {
            DateTime nextTimeToRunQuery;
            return persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery);
        }
    }
}
