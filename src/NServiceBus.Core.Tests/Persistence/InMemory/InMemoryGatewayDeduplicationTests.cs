namespace NServiceBus.Persistence.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

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
    }
}