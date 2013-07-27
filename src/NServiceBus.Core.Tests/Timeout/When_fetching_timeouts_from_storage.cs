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

    [TestFixture]
    [Explicit]
    public class When_fetching_timeouts_from_storage_with_raven : When_fetching_timeouts_from_storage
    {
        private IDocumentStore store;

        protected override IPersistTimeouts CreateTimeoutPersister()
        {
            store = new EmbeddableDocumentStore {RunInMemory = true};
            //store = new DocumentStore { Url = "http://localhost:8080", DefaultDatabase = "TempTest" };
            store.Conventions.DefaultQueryingConsistency = ConsistencyOptions.QueryYourWrites;
            store.Conventions.MaxNumberOfRequestsPerSession = 10;
            store.Initialize();

            store.JsonRequestFactory.DisableRequestCompression = true;
           
            return new RavenTimeoutPersistence(new StoreAccessor(store));
        }

        [TearDown]
        public void Cleanup()
        {
            store.Dispose();
        }

        [Test]
        public void Should_only_return_timeouts_for_this_specific_endpoint_and_any_ones_without_a_owner()
        {
            const int numberOfTimeoutsToAdd = 3;

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                var d = new TimeoutData
                {
                    Time = DateTime.UtcNow.AddHours(-1),
                    OwningTimeoutManager = Configure.EndpointName
                };

                persister.Add(d);
            }

            persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddHours(-1),
                OwningTimeoutManager = "MyOtherTM"
            });

            persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddHours(-1),
                OwningTimeoutManager = String.Empty,
            });

            Assert.AreEqual(numberOfTimeoutsToAdd + 1, GetNextChunk().Count);
        }
    }

    [TestFixture]
    public class When_fetching_timeouts_from_storage_with_inmemory : When_fetching_timeouts_from_storage
    {
        protected override IPersistTimeouts CreateTimeoutPersister()
        {
            return new InMemoryTimeoutPersistence();
        }
    }

    public abstract class When_fetching_timeouts_from_storage
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
        public void Should_only_return_timeouts_for_time_slice()
        {
            const int numberOfTimeoutsToAdd = 10;

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                persister.Add(new TimeoutData
                {
                    OwningTimeoutManager = String.Empty,
                    Time = DateTime.UtcNow.AddHours(-1)
                });
            }

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                persister.Add(new TimeoutData
                {
                    OwningTimeoutManager = String.Empty,
                    Time = DateTime.UtcNow.AddHours(1)
                });
            }
            
            Assert.AreEqual(numberOfTimeoutsToAdd, GetNextChunk().Count);
        }

        [Test]
        public void Should_set_the_next_run()
        {
            const int numberOfTimeoutsToAdd = 50;

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                var d = new TimeoutData
                {
                    Time = DateTime.UtcNow.AddHours(-1),
                    OwningTimeoutManager = Configure.EndpointName
                };

                persister.Add(d);
            }

            var expected = DateTime.UtcNow.AddHours(1);
            persister.Add(new TimeoutData
            {
                Time = expected,
                OwningTimeoutManager = String.Empty,
            });

            DateTime nextTimeToRunQuery;
            persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery);

            var totalMilliseconds = (expected - nextTimeToRunQuery).Duration().TotalMilliseconds;

            Assert.True(totalMilliseconds < 200);
        }

        protected List<Tuple<string, DateTime>> GetNextChunk()
        {
            DateTime nextTimeToRunQuery;
            return persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery);
        }
    }
}