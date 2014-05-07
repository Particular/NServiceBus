namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Persistence.InMemory.TimeoutPersister;
    using NServiceBus.Persistence.Raven;
    using NServiceBus.Persistence.Raven.TimeoutPersister;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Embedded;

//    [TestFixture]
//    public class When_removing_timeouts_from_the_storage_with_raven : When_removing_timeouts_from_the_storage
//    {
//        private IDocumentStore store;
//
//        protected override IPersistTimeouts CreateTimeoutPersister()
//        {
//            store = new EmbeddableDocumentStore {RunInMemory = true};
//            //store = new DocumentStore { Url = "http://localhost:8080", DefaultDatabase = "TempTest" };
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

        protected abstract IPersistTimeouts CreateTimeoutPersister();

        [SetUp]
        public void Setup()
        {
            Address.InitializeLocalAddress("MyEndpoint");

            Configure.GetEndpointNameAction = () => "MyEndpoint";

            persister = CreateTimeoutPersister();
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

        protected List<Tuple<string, DateTime>> GetNextChunk()
        {
            DateTime nextTimeToRunQuery;
            return persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery);
        }
    }
}
