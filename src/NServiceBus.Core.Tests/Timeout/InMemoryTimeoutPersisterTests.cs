namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Linq;
    using NServiceBus.InMemory.TimeoutPersister;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    [TestFixture]
    public class InMemoryTimeoutPersisterTests
    {
        [Test]
        public void When_empty_NextTimeToRunQuery_is_1_minute()
        {
            DateTime nextTimeToRunQuery;
            var now = DateTime.UtcNow;
            new InMemoryTimeoutPersister().GetNextChunk(now, out nextTimeToRunQuery);
            Assert.That(nextTimeToRunQuery, Is.EqualTo(now.AddMinutes(1)).Within(100).Milliseconds);
        }

        [Test]
        public void When_multiple_NextTimeToRunQuery_is_min_date()
        {
            DateTime nextTimeToRunQuery;
            var now = DateTime.UtcNow;
            var persister = new InMemoryTimeoutPersister();
            persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(2)
                          });
            var expectedDate = DateTime.Now.AddDays(1);
            persister.Add(new TimeoutData
                          {
                              Time = expectedDate
                          });
            persister.GetNextChunk(now, out nextTimeToRunQuery);
            Assert.AreEqual(expectedDate, nextTimeToRunQuery);
        }

        [Test]
        public void When_multiple_future_are_returned()
        {
            DateTime nextTime;
            var persister = new InMemoryTimeoutPersister();
            persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-2)
                          });
            persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-4)
                          });
            persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-1)
                          });
            var nextChunk = persister.GetNextChunk(DateTime.Now.AddDays(-3), out nextTime);
            Assert.AreEqual(2, nextChunk.Count());
        }

        [Test]
        public void When_existing_is_removed_existing_is_outted()
        {
            var persister = new InMemoryTimeoutPersister();
            var inputTimeout = new TimeoutData();
            persister.Add(inputTimeout);
            TimeoutData removedTimeout;
            var removed = persister.TryRemove(inputTimeout.Id, out removedTimeout);
            Assert.IsTrue(removed);
            Assert.AreSame(inputTimeout, removedTimeout);
        }

        [Test]
        public void When_existing_is_removed_by_saga_id()
        {
            var persister = new InMemoryTimeoutPersister();
            var newGuid = Guid.NewGuid();
            var inputTimeout = new TimeoutData
                               {
                                   SagaId = newGuid
                               };
            persister.Add(inputTimeout);

            persister.RemoveTimeoutBy(newGuid);
            TimeoutData removedTimeout;
            var removed = persister.TryRemove(inputTimeout.Id, out removedTimeout);
            Assert.False(removed);
        }

        [Test]
        public void When_all_in_past_NextTimeToRunQuery_is_1_minute()
        {
            DateTime nextTimeToRunQuery;
            var now = DateTime.UtcNow;
            var persister = new InMemoryTimeoutPersister();
            persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-1)
                          });
            persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-3)
                          });
            persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-2)
                          });
            persister.GetNextChunk(now, out nextTimeToRunQuery);
            Assert.That(nextTimeToRunQuery, Is.EqualTo(now.AddMinutes(1)).Within(100).Milliseconds);
        }
    }
}