namespace NServiceBus.Persistence.InMemory.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    [TestFixture]
    class InMemorySubscriptionStorageTests
    {
        [Test]
        public async Task Should_ignore_message_version_on_subscriptions()
        {
            var storage = new InMemorySubscriptionStorage();

            await storage.Subscribe(new Subscriber("subscriberA@server1", "subscriberA"), new MessageType("SomeMessage", "1.0.0"), new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                new MessageType("SomeMessage", "2.0.0")
            }, new ContextBag());

            Assert.AreEqual("subscriberA", subscribers.Single().Endpoint);
        }
    }
}