namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Generic;
    using InMemory.TimeoutPersister;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    [TestFixture]
    public class When_fetching_timeouts_from_storage_with_inMemory : When_fetching_timeouts_from_storage
    {
        protected override IPersistTimeouts CreateTimeoutPersister()
        {
            return new InMemoryTimeoutPersister();
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


            persister = CreateTimeoutPersister();
        }

        [Test]
        public void Should_only_return_timeouts_for_time_slice()
        {
            const int numberOfTimeoutsToAdd = 10;

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                persister.Add("Id-" + i,new TimeoutData
                {
                    OwningTimeoutManager = String.Empty,
                    Time = DateTime.UtcNow.AddHours(-1)
                });
            }

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                persister.Add("Id-" + i,new TimeoutData
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
                    OwningTimeoutManager = "MyEndpoint"
                };

                persister.Add("Id-" + i,d);
            }

            var expected = DateTime.UtcNow.AddHours(1);
            persister.Add("OtherId",new TimeoutData
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