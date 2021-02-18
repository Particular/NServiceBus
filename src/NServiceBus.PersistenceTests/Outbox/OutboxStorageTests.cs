namespace NServiceBus.PersistenceTesting.Outbox
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using NUnit.Framework;

    [TestFixtureSource(typeof(PersistenceTestsConfiguration), nameof(PersistenceTestsConfiguration.OutboxVariants))]
    class OutboxStorageTests
    {
        public OutboxStorageTests(TestVariant param)
        {
            this.param = param;
        }

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            configuration = new PersistenceTestsConfiguration(param);
            await configuration.Configure();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await configuration.Cleanup();
        }

        [Test]
        public async Task Should_clear_operations_on_dispatched_messages()
        {
            configuration.RequiresOutboxSupport();

            var storage = configuration.OutboxStorage;
            var ctx = configuration.GetContextBagForOutbox();

            var messageId = Guid.NewGuid().ToString();
            await storage.Get(messageId, ctx, default);

            var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation("x", null, null, null) });
            using (var transaction = await storage.BeginTransaction(ctx, default))
            {
                await storage.Store(messageToStore, transaction, ctx, default);

                await transaction.Commit(default);
            }

            await storage.SetAsDispatched(messageId, ctx, default);

            var message = await storage.Get(messageId, configuration.GetContextBagForOutbox(), default);

            Assert.That(message, Is.Not.Null);
            CollectionAssert.IsEmpty(message.TransportOperations);
        }

        [Test]
        public async Task Should_throw_if_trying_to_insert_same_messageid()
        {
            configuration.RequiresOutboxSupport();

            var failed = false;

            var storage = configuration.OutboxStorage;
            var winningContextBag = configuration.GetContextBagForOutbox();
            var losingContextBag = configuration.GetContextBagForOutbox();
            await storage.Get("MySpecialId", winningContextBag, default);
            await storage.Get("MySpecialId", losingContextBag, default);

            using (var transactionA = await storage.BeginTransaction(winningContextBag, default))
            {
                await storage.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), transactionA, winningContextBag, default);
                await transactionA.Commit(default);
            }

            try
            {
                using (var transactionB = await storage.BeginTransaction(losingContextBag, default))
                {
                    await storage.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), transactionB, losingContextBag, default);
                    await transactionB.Commit(default);
                }
            }
            catch (Exception)
            {
                failed = true;
            }
            Assert.IsTrue(failed);
        }

        [Test]
        public async Task Should_not_store_when_transaction_not_commited()
        {
            configuration.RequiresOutboxSupport();

            var storage = configuration.OutboxStorage;
            var ctx = configuration.GetContextBagForOutbox();

            var messageId = Guid.NewGuid().ToString();
            await storage.Get(messageId, ctx, default);

            using (var transaction = await storage.BeginTransaction(ctx, default))
            {
                var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation("x", null, null, null) });
                await storage.Store(messageToStore, transaction, ctx, default);

                // do not commit
            }

            var message = await storage.Get(messageId, configuration.GetContextBagForOutbox(), default);
            Assert.Null(message);
        }

        [Test]
        public async Task Should_store_when_transaction_commited()
        {
            configuration.RequiresOutboxSupport();

            var storage = configuration.OutboxStorage;
            var ctx = configuration.GetContextBagForOutbox();

            var messageId = Guid.NewGuid().ToString();
            await storage.Get(messageId, ctx, default);

            using (var transaction = await storage.BeginTransaction(ctx, default))
            {
                var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation("x", null, null, null) });
                await storage.Store(messageToStore, transaction, ctx, default);

                await transaction.Commit(default);
            }

            var message = await storage.Get(messageId, configuration.GetContextBagForOutbox(), default);
            Assert.NotNull(message);
        }

        IPersistenceTestsConfiguration configuration;
        TestVariant param;
    }
}