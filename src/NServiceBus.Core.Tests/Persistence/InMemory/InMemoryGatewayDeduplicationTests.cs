namespace NServiceBus.Persistence.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Features;
    using Gateway.Deduplication;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    class InMemoryGatewayDeduplicationTests
    {
        [Test]
        public async Task Should_return_true_on_first_unique_test()
        {
            var storage = new InMemoryGatewayDeduplication(2);

            await storage.DeduplicateMessage("A", DateTime.UtcNow, new ContextBag());
            Assert.True(await storage.DeduplicateMessage("B", DateTime.UtcNow, new ContextBag()));
        }    

        [Test]
        public async Task Should_return_false_on_second_test()
        {
            var storage = new InMemoryGatewayDeduplication(2);

            await storage.DeduplicateMessage("A", DateTime.UtcNow, new ContextBag());
            Assert.False(await storage.DeduplicateMessage("A", DateTime.UtcNow, new ContextBag()));
        }    
        
        [Test]
        public async Task Should_return_true_if_LRU_reaches_limit()
        {
            var storage = new InMemoryGatewayDeduplication(2);

            await storage.DeduplicateMessage("A", DateTime.UtcNow, new ContextBag());
            await storage.DeduplicateMessage("B", DateTime.UtcNow, new ContextBag());
            await storage.DeduplicateMessage("C", DateTime.UtcNow, new ContextBag());
            Assert.True(await storage.DeduplicateMessage("A", DateTime.UtcNow, new ContextBag()));
        }

        [Test]
        public void Should_have_configured_maxsize()
        {
            var feature = new InMemoryGatewayPersistence();
            var settings = new SettingsHolder();
            var container = new CommonObjectBuilder(new LightInjectObjectBuilder());

            var persistenceSettings = new PersistenceExtensions<InMemoryPersistence>(settings);
            persistenceSettings.GatewayDeduplicationCacheSize(42);

            feature.Setup(new FeatureConfigurationContext(settings, container, null, null, null));

            var implementation = (InMemoryGatewayDeduplication)container.Build<IDeduplicateMessages>();
            Assert.AreEqual(42, implementation.maxSize);
        }

    }
}