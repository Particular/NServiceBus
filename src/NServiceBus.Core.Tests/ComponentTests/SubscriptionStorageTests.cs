namespace NServiceBus.Persistence.InMemory.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using ComponentTests;
    using NUnit.Framework;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    [TestFixture]
    class SubscriptionStorageTests
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
        public async Task Should_ignore_message_version_on_subscriptions()
        {
            var storage = configuration.SubscriptionStorage;

            await storage.Subscribe(new Subscriber("subscriberA@server1", "subscriberA"), new MessageType("SomeMessage", "1.0.0"), configuration.GetContextBagForSubscriptions());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                new MessageType("SomeMessage", "2.0.0")
            }, configuration.GetContextBagForSubscriptions());

            Assert.AreEqual("subscriberA", subscribers.Single().Endpoint);
        }
    }
}