namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using InMemory.TimeoutPersister;
    using NServiceBus.Extensibility;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    [TestFixture]
    public class When_fetching_timeouts_from_storage_with_inMemory
    {
        InMemoryTimeoutPersister persister;
        TimeoutPersistenceOptions options;

        [SetUp]
        public void Setup()
        {
            options = new TimeoutPersistenceOptions(new ContextBag());
            persister = new InMemoryTimeoutPersister();
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
                }, options);
            }

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                persister.Add(new TimeoutData
                {
                    OwningTimeoutManager = String.Empty,
                    Time = DateTime.UtcNow.AddHours(1)
                }, options);
            }

            Assert.AreEqual(numberOfTimeoutsToAdd, GetNextChunk().Count());
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

                persister.Add(d, options);
            }

            var expected = DateTime.UtcNow.AddHours(1);
            persister.Add(new TimeoutData
            {
                Time = expected,
                OwningTimeoutManager = String.Empty,
            }, options);

            DateTime nextTimeToRunQuery;
            persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery);

            var totalMilliseconds = (expected - nextTimeToRunQuery).Duration().TotalMilliseconds;

            Assert.True(totalMilliseconds < 200);
        }

        IEnumerable<Tuple<string, DateTime>> GetNextChunk()
        {
            DateTime nextTimeToRunQuery;
            return persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery);
        }
    }
}