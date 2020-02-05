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

#pragma warning disable 0618
            persistenceSettings.GatewayDeduplicationCacheSize(42);

            Assert.AreEqual(42, settings.Get<int>(InMemoryGatewayPersistence.MaxSizeKey));
#pragma warning restore 0618
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

            using (new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                await storage.DeduplicateMessage("A", DateTime.UtcNow, new ContextBag());

                // no commit
            }

            Assert.True(await storage.DeduplicateMessage("A", DateTime.UtcNow, new ContextBag()));
        }

        [Test]
        // With the gateway persistence seam v1 design it's only safe to deduplicate when there is a tx scope present since the check happens before
        // the messages have been pushed to the transport. If we add entries earlier they would be considered duplicate when retrying after something went wrong.
        // Note: The gateway will always wrap the v1 seam invocation in a transaction scope
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
