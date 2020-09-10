namespace NServiceBus.PersistenceTesting.Outbox
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using NUnit.Framework;

    [TestFixtureSource(typeof(PersistenceTestsConfiguration), "OutboxVariants")]
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
            await storage.Get(messageId, ctx, CancellationToken.None);

            var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation("x", null, null, null) });
            using (var transaction = await storage.BeginTransaction(ctx, CancellationToken.None))
            {
                await storage.Store(messageToStore, transaction, ctx);

                await transaction.Commit(CancellationToken.None);
            }

            await storage.SetAsDispatched(messageId, configuration.GetContextBagForOutbox());

            var message = await storage.Get(messageId, configuration.GetContextBagForOutbox(), CancellationToken.None);

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
            await storage.Get("MySpecialId", winningContextBag, CancellationToken.None);
            await storage.Get("MySpecialId", losingContextBag, CancellationToken.None);

            using (var transactionA = await storage.BeginTransaction(winningContextBag, CancellationToken.None))
            {
                await storage.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), transactionA, winningContextBag);
                await transactionA.Commit(CancellationToken.None);
            }

            try
            {
                using (var transactionB = await storage.BeginTransaction(losingContextBag, CancellationToken.None))
                {
                    await storage.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), transactionB, losingContextBag);
                    await transactionB.Commit(CancellationToken.None);
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
            await storage.Get(messageId, ctx, CancellationToken.None);

            using (var transaction = await storage.BeginTransaction(ctx, CancellationToken.None))
            {
                var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation("x", null, null, null) });
                await storage.Store(messageToStore, transaction, ctx);

                // do not commit
            }

            var message = await storage.Get(messageId, configuration.GetContextBagForOutbox(), CancellationToken.None);
            Assert.Null(message);
        }

        [Test]
        public async Task Should_store_when_transaction_commited()
        {
            configuration.RequiresOutboxSupport();

            var storage = configuration.OutboxStorage;
            var ctx = configuration.GetContextBagForOutbox();

            var messageId = Guid.NewGuid().ToString();
            await storage.Get(messageId, ctx, CancellationToken.None);

            using (var transaction = await storage.BeginTransaction(ctx, CancellationToken.None))
            {
                var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation("x", null, null, null) });
                await storage.Store(messageToStore, transaction, ctx);

                await transaction.Commit(CancellationToken.None);
            }

            var message = await storage.Get(messageId, configuration.GetContextBagForOutbox(), CancellationToken.None);
            Assert.NotNull(message);
        }

        IPersistenceTestsConfiguration configuration;
        TestVariant param;
    }
}