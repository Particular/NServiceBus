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
        public void Should_evict_oldest_entry_when_LRU_reaches_limit()
        {
            var clientIdStorage = new ClientIdStorage(2);

            clientIdStorage.RegisterClientId("A");
            clientIdStorage.RegisterClientId("B");
            clientIdStorage.RegisterClientId("C");

            Assert.False(clientIdStorage.IsDuplicate("A"));
        }

        [Test]
        public void Should_reset_time_added_for_existing_ids_when_checked()
        {
            var clientIdStorage = new ClientIdStorage(2);

            clientIdStorage.RegisterClientId("A");
            clientIdStorage.RegisterClientId("B");

            Assert.True(clientIdStorage.IsDuplicate("A"));

            clientIdStorage.RegisterClientId("C");

            Assert.False(clientIdStorage.IsDuplicate("B"));
            Assert.True(clientIdStorage.IsDuplicate("A"));
        }

        [Test]
        public void Should_have_configured_maxsize()
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
        // With this design it's only safe to deuplicate when there is a tx scope present since we check before
        // the message have been pushed to the transport. If we add entries at here we will loose them should there be
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