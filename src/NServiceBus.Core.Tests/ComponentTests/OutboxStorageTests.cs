namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Outbox;

    [TestFixture]
    class OutboxStorageTests
    {
        PersistenceTestsConfiguration configuration;

        [OneTimeSetUp]
        public Task OneTimeSetUp()
        {
            configuration = new PersistenceTestsConfiguration();
            return configuration.Configure();
        }

        [OneTimeTearDown]
        public Task OneTimeTearDown()
        {
            return configuration.Cleanup();
        }

        [Test]
        public async Task Should_clear_operations_on_dispatched_messages()
        {
            var storage = configuration.OutboxStorage;
            var ctx = configuration.GetContextBagForOutbox();

            var messageId = Guid.NewGuid().ToString();

            var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation("x", null, null, null) });
            using (var transaction = await storage.BeginTransaction(ctx))
            {
                await storage.Store(messageToStore, transaction, ctx);

                await transaction.Commit();
            }

            await storage.SetAsDispatched(messageId, configuration.GetContextBagForOutbox());

            var message = await storage.Get(messageId, configuration.GetContextBagForOutbox());

            Assert.False(message.TransportOperations.Any());
        }

        [Test]
        public async Task Should_not_remove_non_dispatched_messages()
        {
            var storage = configuration.OutboxStorage;
            var ctx = configuration.GetContextBagForOutbox();

            var messageId = Guid.NewGuid().ToString();

            var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation("x", null, null, null) });

            using (var transaction = await storage.BeginTransaction(ctx))
            {
                await storage.Store(messageToStore, transaction, ctx);

                await transaction.Commit();
            }

            await CleanOlderThan(storage, DateTimeOffset.UtcNow);

            var message = await storage.Get(messageId, configuration.GetContextBagForOutbox());
            Assert.NotNull(message);
        }

        [Test]
        public async Task Should_clear_dispatched_messages_after_given_expiry()
        {
            var storage = configuration.OutboxStorage;
            var ctx = configuration.GetContextBagForOutbox();

            var messageId = Guid.NewGuid().ToString();

            var beforeStore = DateTimeOffset.UtcNow;

            var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation("x", null, null, null) });
            using (var transaction = await storage.BeginTransaction(ctx))
            {
                await storage.Store(messageToStore, transaction, ctx);

                await transaction.Commit();
            }

            // Account for the low resolution of DateTime.UtcNow.
            var afterStore = DateTimeOffset.UtcNow.AddTicks(1);

            await storage.SetAsDispatched(messageId, configuration.GetContextBagForOutbox());

            await CleanOlderThan(storage, beforeStore);
            
            var message = await storage.Get(messageId, configuration.GetContextBagForOutbox());
            Assert.NotNull(message);

            await CleanOlderThan(storage, afterStore);

            message = await storage.Get(messageId, configuration.GetContextBagForOutbox());
            Assert.Null(message);
        }

        [Test]
        public async Task Should_not_store_when_transaction_not_commited()
        {
            var storage = configuration.OutboxStorage;
            var ctx = configuration.GetContextBagForOutbox();

            var messageId = Guid.NewGuid().ToString();

            using (var transaction = await storage.BeginTransaction(ctx))
            {
                var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation("x", null, null, null) });
                await storage.Store(messageToStore, transaction, ctx);

                // do not commit
            }

            var message = await storage.Get(messageId, configuration.GetContextBagForOutbox());
            Assert.Null(message);
        }

        [Test]
        public async Task Should_store_when_transaction_commited()
        {
            var storage = configuration.OutboxStorage;
            var ctx = configuration.GetContextBagForOutbox();

            var messageId = Guid.NewGuid().ToString();

            using (var transaction = await storage.BeginTransaction(ctx))
            {
                var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation("x", null, null, null) });
                await storage.Store(messageToStore, transaction, ctx);

                await transaction.Commit();
            }

            var message = await storage.Get(messageId, configuration.GetContextBagForOutbox());
            Assert.NotNull(message);
        }


        Task CleanOlderThan(IOutboxStorage storage, DateTimeOffset beforeStore)
        {
            return TaskEx.CompletedTask;
        }
    }
}