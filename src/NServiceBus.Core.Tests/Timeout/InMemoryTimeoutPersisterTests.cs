namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    [TestFixture]
    public class InMemoryTimeoutPersisterTests
    {
        [Test]
        public async Task When_empty_NextTimeToRunQuery_is_1_minute()
        {
            var now = DateTime.UtcNow;
            var persister = new InMemoryTimeoutPersister(() => DateTime.UtcNow);
            var result = await persister.GetNextChunk(now);
            Assert.That(result.NextTimeToQuery, Is.EqualTo(now.AddMinutes(1)).Within(100).Milliseconds);
        }

        [Test]
        public async Task When_multiple_NextTimeToRunQuery_is_min_date()
        {
            var now = DateTime.UtcNow;
            var persister = new InMemoryTimeoutPersister(() => DateTime.UtcNow);
            await persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddDays(2)
            }, new ContextBag());
            var expectedDate = DateTime.UtcNow.AddDays(1);
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
            var persister = new InMemoryTimeoutPersister(() => DateTime.UtcNow);
            await persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddDays(-2)
            }, new ContextBag());
            await persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddDays(-4)
            }, new ContextBag());
            await persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddDays(-1)
            }, new ContextBag());

            var result = await persister.GetNextChunk(DateTime.UtcNow.AddDays(-3));

            Assert.AreEqual(2, result.DueTimeouts.Count());
        }

        [Test]
        public async Task TryRemove_when_existing_is_removed_should_return_true()
        {
            var persister = new InMemoryTimeoutPersister(() => DateTime.UtcNow);
            var inputTimeout = new TimeoutData();
            await persister.Add(inputTimeout, new ContextBag());

            var result = await persister.TryRemove(inputTimeout.Id, new ContextBag());

            Assert.IsTrue(result);
        }

        [Test]
        public async Task TryRemove_when_non_existing_is_removed_should_return_false()
        {
            var persister = new InMemoryTimeoutPersister(() => DateTime.UtcNow);
            var inputTimeout = new TimeoutData();
            await persister.Add(inputTimeout, new ContextBag());

            var result = await persister.TryRemove(Guid.NewGuid().ToString(), new ContextBag());

            Assert.False(result);
        }

        [Test]
        public async Task Peek_when_timeout_exists_should_return_timeout()
        {
            var persister = new InMemoryTimeoutPersister(() => DateTime.UtcNow);
            var inputTimeout = new TimeoutData();
            await persister.Add(inputTimeout, new ContextBag());

            var result = await persister.Peek(inputTimeout.Id, new ContextBag());

            Assert.AreSame(inputTimeout, result);
        }

        [Test]
        public async Task Peek_when_timeout_does_not_exist_should_return_null()
        {
            var persister = new InMemoryTimeoutPersister(() => DateTime.UtcNow);
            var inputTimeout = new TimeoutData();
            await persister.Add(inputTimeout, new ContextBag());

            var result = await persister.Peek(Guid.NewGuid().ToString(), new ContextBag());

            Assert.IsNull(result);
        }

        [Test]
        public async Task When_existing_is_removed_by_saga_id()
        {
            var persister = new InMemoryTimeoutPersister(() => DateTime.UtcNow);
            var newGuid = Guid.NewGuid();
            var inputTimeout = new TimeoutData
            {
                SagaId = newGuid
            };

            await persister.Add(inputTimeout, new ContextBag());
            await persister.RemoveTimeoutBy(newGuid, new ContextBag());
            var result = await persister.TryRemove(inputTimeout.Id, new ContextBag());

            Assert.IsFalse(result);
        }

        [Test]
        public async Task When_all_in_past_NextTimeToRunQuery_is_1_minute()
        {
            var now = DateTime.UtcNow;
            var persister = new InMemoryTimeoutPersister(() => DateTime.UtcNow);
            await persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddDays(-1)
            }, new ContextBag());
            await persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddDays(-3)
            }, new ContextBag());
            await persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddDays(-2)
            }, new ContextBag());

            var result = await persister.GetNextChunk(now);

            Assert.That(result.NextTimeToQuery, Is.EqualTo(now.AddMinutes(1)).Within(100).Milliseconds);
        }
    }
}