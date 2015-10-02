namespace NServiceBus.Persistence.InMemory.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.InMemory.Outbox;
    using NUnit.Framework;
    using Outbox;

    [TestFixture]
    class InMemoryOutboxPersisterTests
    {
        [Test]
        public async Task Should_clear_operations_on_dispatched_messages()
        {
            var storage = new InMemoryOutboxStorage();

            var messageId = "myId";

            var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation("x", null, null, null) });
            await storage.Store(messageToStore, new OutboxStorageOptions(new ContextBag()));
            await storage.SetAsDispatched(messageId, new OutboxStorageOptions(new ContextBag()));

            var message = await storage.Get(messageId, new OutboxStorageOptions(new ContextBag()));

            Assert.False(message.TransportOperations.Any());
        }

        [Test]
        public async Task Should_not_remove_non_dispatched_messages()
        {
            var storage = new InMemoryOutboxStorage();

            var messageId = "myId";

            var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation("x", null, null, null) });

            await storage.Store(messageToStore, new OutboxStorageOptions(new ContextBag()));

            storage.RemoveEntriesOlderThan(DateTime.UtcNow);

            var message = await storage.Get(messageId, new OutboxStorageOptions(new ContextBag()));
            Assert.NotNull(message);
        }

        [Test]
        public async Task Should_clear_dispatched_messages_after_given_expiry()
        {
            var storage = new InMemoryOutboxStorage();

            var messageId = "myId";

            var beforeStore = DateTime.UtcNow;

            var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation("x", null, null, null) });

            await storage.Store(messageToStore, new OutboxStorageOptions(new ContextBag()));

            await storage.SetAsDispatched(messageId, new OutboxStorageOptions(new ContextBag()));

            storage.RemoveEntriesOlderThan(beforeStore);

            var message = await storage.Get(messageId, new OutboxStorageOptions(new ContextBag()));
            Assert.NotNull(message);

            storage.RemoveEntriesOlderThan(DateTime.UtcNow);

            message = await storage.Get(messageId, new OutboxStorageOptions(new ContextBag()));
            Assert.Null(message);
        }
    }
}