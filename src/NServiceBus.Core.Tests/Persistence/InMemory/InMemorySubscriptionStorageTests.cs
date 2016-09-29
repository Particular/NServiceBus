namespace NServiceBus.Persistence.InMemory.Tests
{
    using System.Linq;
    using NServiceBus.InMemory.SubscriptionStorage;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;

    [TestFixture]
    class InMemorySubscriptionStorageTests
    {
        [Test]
        public void Should_ignore_message_version_on_subscriptions()
        {
            ISubscriptionStorage storage = new InMemorySubscriptionStorage();

            storage.Subscribe(new Address("subscriberA", "subscriberA"), new[]
            {
                new MessageType("SomeMessage", "1.0.0")
            });

            var subscribers = storage.GetSubscriberAddressesForMessage(new[]
            {
                new MessageType("SomeMessage", "2.0.0")
            });

            Assert.AreEqual("subscriberA", subscribers.Single().Queue);
        }
    }
}