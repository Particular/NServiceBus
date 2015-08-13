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
    public class When_removing_timeouts_from_the_storage_with_inMemory
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
        public void Should_remove_timeouts_by_id()
        {
            var t1 = new TimeoutData { Id = "1", Time = DateTime.UtcNow.AddHours(-1) };
            persister.Add(t1, options);

            var t2 = new TimeoutData { Id = "2", Time = DateTime.UtcNow.AddHours(-1) };
            persister.Add(t2, options);

            var timeouts = GetNextChunk();

            foreach (var timeout in timeouts)
            {
                TimeoutData timeoutData;
                persister.TryRemove(timeout.Item1, options, out timeoutData);
            }

            Assert.AreEqual(0, GetNextChunk().Count());
        }

        IEnumerable<Tuple<string, DateTime>> GetNextChunk()
        {
            DateTime nextTimeToRunQuery;
            return persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery);
        }
    }
}
