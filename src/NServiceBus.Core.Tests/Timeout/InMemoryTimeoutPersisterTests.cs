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
            var options = new TimeoutPersistenceOptions(new ContextBag());
            var persister = new InMemoryTimeoutPersister();
            await persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(2)
                          }, options);
            var expectedDate = DateTime.Now.AddDays(1);
            await persister.Add(new TimeoutData
                          {
                              Time = expectedDate
                          }, options);

            var result = await persister.GetNextChunk(now);

            Assert.AreEqual(expectedDate, result.NextTimeToQuery);
        }

        [Test]
        public async Task When_multiple_future_are_returned()
        {
            var options = new TimeoutPersistenceOptions(new ContextBag());
            var persister = new InMemoryTimeoutPersister();
            await persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-2)
                          }, options);
            await persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-4)
                          }, options);
            await persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-1)
                          }, options);

            var result = await persister.GetNextChunk(DateTime.Now.AddDays(-3));

            Assert.AreEqual(2, result.DueTimeouts.Count());
        }

        [Test]
        public async Task When_existing_is_removed_by_id()
        {
            var options = new TimeoutPersistenceOptions(new ContextBag());
            var persister = new InMemoryTimeoutPersister();

            var timeoutId = await persister.Add(new TimeoutData(), options);
            await persister.Remove(timeoutId, options);
            var result = await persister.Peek(timeoutId, options);

            Assert.IsNull(result);
        }

        [Test]
        public async Task When_existing_is_removed_by_saga_id()
        {
            var options = new TimeoutPersistenceOptions(new ContextBag());
            var persister = new InMemoryTimeoutPersister();
            var newGuid = Guid.NewGuid();
            var inputTimeout = new TimeoutData
                               {
                                   SagaId = newGuid
                               };
            
            var timeoutId = await persister.Add(inputTimeout, options);
            await persister.Remove(timeoutId, options);
            var result = await persister.Peek(timeoutId, options);

            Assert.IsNull(result);
        }

        [Test]
        public async Task When_all_in_past_NextTimeToRunQuery_is_1_minute()
        {
            var now = DateTime.UtcNow;
            var options = new TimeoutPersistenceOptions(new ContextBag());
            var persister = new InMemoryTimeoutPersister();
            await persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-1)
                          }, options);
            await persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-3)
                          }, options);
            await persister.Add(new TimeoutData
                          {
                              Time = DateTime.Now.AddDays(-2)
                          }, options);

            var result = await persister.GetNextChunk(now);

            Assert.That(result.NextTimeToQuery, Is.EqualTo(now.AddMinutes(1)).Within(100).Milliseconds);
        }
    }
}