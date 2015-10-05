namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.InMemory.TimeoutPersister;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    [TestFixture]
    public class InMemoryTimeoutPersisterTests
    {
        [Test]
        public async Task When_empty_NextTimeToRunQuery_is_1_minute()
        {
            var now = DateTime.UtcNow;
            var persister = new InMemoryTimeoutPersister();
            
            var result = await persister.GetNextChunk(now);
            
            Assert.That(result.NextTimeToQuery, Is.EqualTo(now.AddMinutes(1)).Within(100).Milliseconds);
        }

        [Test]
        public async Task When_multiple_NextTimeToRunQuery_is_min_date()
        {
            var now = DateTime.UtcNow;
            var persister = new InMemoryTimeoutPersister();
            await persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(2)
                          }, new ContextBag());
            var expectedDate = DateTime.Now.AddDays(1);
            await persister.Add(new TimeoutData
                          {
                              Time = expectedDate
                          }, new ContextBag());

            var result = await persister.GetNextChunk(now);

            Assert.AreEqual(expectedDate, result.NextTimeToQuery);
        }

        [Test]
        public async Task When_multiple_future_are_returned()
        {
            var persister = new InMemoryTimeoutPersister();
            await persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-2)
                          }, new ContextBag());
            await persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-4)
                          }, new ContextBag());
            await persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-1)
                          }, new ContextBag());

            var result = await persister.GetNextChunk(DateTime.Now.AddDays(-3));

            Assert.AreEqual(2, result.DueTimeouts.Count());
        }

        [Test]
        public async Task When_existing_is_removed_existing_is_outted()
        {
           var persister = new InMemoryTimeoutPersister();
            var inputTimeout = new TimeoutData();

            await persister.Add(inputTimeout, new ContextBag());
            var result = await persister.Remove(inputTimeout.Id, new ContextBag());
            
            Assert.AreSame(inputTimeout, result);
        }

        [Test]
        public async Task When_existing_is_removed_by_saga_id()
        {
            var persister = new InMemoryTimeoutPersister();
            var newGuid = Guid.NewGuid();
            var inputTimeout = new TimeoutData
                               {
                                   SagaId = newGuid
                               };
            
            await persister.Add(inputTimeout, new ContextBag());
            await persister.RemoveTimeoutBy(newGuid, new ContextBag());
            var result = await persister.Remove(inputTimeout.Id, new ContextBag());

            Assert.IsNull(result);
        }

        [Test]
        public async Task When_all_in_past_NextTimeToRunQuery_is_1_minute()
        {
            var now = DateTime.UtcNow;
            var persister = new InMemoryTimeoutPersister();
            await persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-1)
                          }, new ContextBag());
            await persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-3)
                          }, new ContextBag());
            await persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-2)
                          }, new ContextBag());

            var result = await persister.GetNextChunk(now);

            Assert.That(result.NextTimeToQuery, Is.EqualTo(now.AddMinutes(1)).Within(100).Milliseconds);
        }
    }
}