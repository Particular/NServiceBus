namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Timeout.Core;

    [TestFixture]
    public class TimeoutStorageTests
    {
        PersistenceTestsConfiguration configuration;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            configuration = new PersistenceTestsConfiguration();
            await configuration.Configure();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await configuration.Cleanup();
        }

        [Test]
        public async Task When_empty_NextTimeToRunQuery_is_1_minute()
        {
            var now = DateTime.UtcNow;
            var persister = configuration.TimeoutQuery;
            var result = await persister.GetNextChunk(now);
            Assert.That(result.NextTimeToQuery, Is.EqualTo(now.AddMinutes(1)).Within(100).Milliseconds);
        }

        [Test]
        public async Task When_multiple_NextTimeToRunQuery_is_min_date()
        {
            var now = DateTime.UtcNow;
            var persister = configuration.TimeoutStorage;
            await persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddDays(2)
            }, configuration.GetContextBagForTimeoutPersister());
            var expectedDate = DateTime.UtcNow.AddDays(1);
            await persister.Add(new TimeoutData
            {
                Time = expectedDate
            }, configuration.GetContextBagForTimeoutPersister());

            var result = await configuration.TimeoutQuery.GetNextChunk(now);

            Assert.AreEqual(expectedDate, result.NextTimeToQuery);
        }

        [Test]
        public async Task When_multiple_future_are_returned()
        {
            var persister = configuration.TimeoutStorage;
            await persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddDays(-2)
            }, configuration.GetContextBagForTimeoutPersister());
            await persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddDays(-4)
            }, configuration.GetContextBagForTimeoutPersister());
            await persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddDays(-1)
            }, configuration.GetContextBagForTimeoutPersister());

            var result = await configuration.TimeoutQuery.GetNextChunk(DateTime.UtcNow.AddDays(-3));

            Assert.AreEqual(2, result.DueTimeouts.Length);
        }

        [Test]
        public async Task TryRemove_when_existing_is_removed_should_return_true()
        {
            var persister = configuration.TimeoutStorage;
            var inputTimeout = new TimeoutData();
            await persister.Add(inputTimeout, configuration.GetContextBagForTimeoutPersister());

            var result = await persister.TryRemove(inputTimeout.Id, configuration.GetContextBagForTimeoutPersister());

            Assert.IsTrue(result);
        }

        [Test]
        public async Task TryRemove_when_non_existing_is_removed_should_return_false()
        {
            var persister = configuration.TimeoutStorage;
            var inputTimeout = new TimeoutData();
            await persister.Add(inputTimeout, configuration.GetContextBagForTimeoutPersister());

            var result = await persister.TryRemove(Guid.NewGuid().ToString(), new ContextBag());

            Assert.False(result);
        }

        [Test]
        public async Task Peek_when_timeout_exists_should_return_timeout()
        {
            var persister = configuration.TimeoutStorage;
            var inputTimeout = new TimeoutData();
            await persister.Add(inputTimeout, configuration.GetContextBagForTimeoutPersister());

            var result = await persister.Peek(inputTimeout.Id, configuration.GetContextBagForTimeoutPersister());

            Assert.AreSame(inputTimeout, result);
        }

        [Test]
        public async Task Peek_when_timeout_does_not_exist_should_return_null()
        {
            var persister = configuration.TimeoutStorage;
            var inputTimeout = new TimeoutData();
            await persister.Add(inputTimeout, configuration.GetContextBagForTimeoutPersister());

            var result = await persister.Peek(Guid.NewGuid().ToString(), configuration.GetContextBagForTimeoutPersister());

            Assert.IsNull(result);
        }

        [Test]
        public async Task When_existing_is_removed_by_saga_id()
        {
            var persister = configuration.TimeoutStorage;
            var newGuid = Guid.NewGuid();
            var inputTimeout = new TimeoutData
            {
                SagaId = newGuid
            };

            await persister.Add(inputTimeout, configuration.GetContextBagForTimeoutPersister());
            await persister.RemoveTimeoutBy(newGuid, configuration.GetContextBagForTimeoutPersister());
            var result = await persister.TryRemove(inputTimeout.Id, configuration.GetContextBagForTimeoutPersister());

            Assert.IsFalse(result);
        }

        [Test]
        public async Task When_all_in_past_NextTimeToRunQuery_is_1_minute()
        {
            var now = DateTime.UtcNow;
            var persister = configuration.TimeoutStorage;
            await persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddDays(-1)
            }, configuration.GetContextBagForTimeoutPersister());
            await persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddDays(-3)
            }, configuration.GetContextBagForTimeoutPersister());
            await persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddDays(-2)
            }, configuration.GetContextBagForTimeoutPersister());

            var result = await configuration.TimeoutQuery.GetNextChunk(now);

            Assert.That(result.NextTimeToQuery, Is.EqualTo(now.AddMinutes(1)).Within(100).Milliseconds);
        }
    }
}