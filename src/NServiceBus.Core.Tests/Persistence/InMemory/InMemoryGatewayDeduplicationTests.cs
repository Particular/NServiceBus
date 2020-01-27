namespace NServiceBus.Persistence.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using Features;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    class InMemoryGatewayDeduplicationTests
    {
        [Test]
        public void Should_have_configured_storage_maxsize()
        {
            var settings = new SettingsHolder();
            var persistenceSettings = new PersistenceExtensions<InMemoryPersistence>(settings);

            persistenceSettings.GatewayDeduplicationCacheSize(42);

            Assert.AreEqual(42, settings.Get<int>(InMemoryGatewayPersistence.MaxSizeKey));
        }

        [Test]
        public async Task Should_add_on_transaction_scope_commit()
        {
            var storage = CreateInMemoryGatewayDeduplication();

            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                await storage.DeduplicateMessage("A", DateTime.UtcNow, new ContextBag());

                scope.Complete();
            }

            Assert.False(await storage.DeduplicateMessage("A", DateTime.UtcNow, new ContextBag()));
        }

        [Test]
        public async Task Should_not_add_on_transaction_scope_abort()
        {
            var storage = CreateInMemoryGatewayDeduplication();

            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                await storage.DeduplicateMessage("A", DateTime.UtcNow, new ContextBag());

                // no commit
            }

            Assert.True(await storage.DeduplicateMessage("A", DateTime.UtcNow, new ContextBag()));
        }

        [Test]
        // With this design, it's only safe to deduplicate when there is a tx scope present since we check before
        // the messages have been pushed to the transport. If we add entries here, we will lose them should there be
        // a problem with pushing the message.
        public async Task Should_only_deduplicate_when_scope_is_present()
        {
            var storage = CreateInMemoryGatewayDeduplication();

            await storage.DeduplicateMessage("A", DateTime.UtcNow, new ContextBag());

            Assert.True(await storage.DeduplicateMessage("A", DateTime.UtcNow, new ContextBag()));
        }

        InMemoryGatewayDeduplication CreateInMemoryGatewayDeduplication()
        {
            return new InMemoryGatewayDeduplication(new ClientIdStorage(10));
        }
    }
}
