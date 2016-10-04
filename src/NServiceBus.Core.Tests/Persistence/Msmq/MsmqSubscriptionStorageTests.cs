namespace NServiceBus.Core.Tests.Persistence.Msmq
{
    using System;
    using System.Linq;
    using System.Messaging;
    using NServiceBus.Persistence.SubscriptionStorage;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;
    using MessageType = NServiceBus.Unicast.Subscriptions.MessageType;

    public class MsmqSubscriptionStorageTests
    {
        [Test]
        public void Should_ignore_message_version_on_subscriptions()
        {
            var testQueueName = "ShouldIgnoreMessageVersionOnSubscriptions";
            var testQueueNativeAddress = Environment.MachineName + MsmqUtilities.PRIVATE + testQueueName;

            if (MessageQueue.Exists(testQueueNativeAddress))
            {
                new MessageQueue(testQueueNativeAddress).Purge();
            }
            else
            {
                MessageQueue.Create(testQueueNativeAddress);
            }

            ISubscriptionStorage subscriptionStorage = new MsmqSubscriptionStorage
            {
                Queue = Address.Parse(testQueueName)
            };

            subscriptionStorage.Init();

            subscriptionStorage.Subscribe(new Address("subscriberA", "server1"), new[] { new MessageType("SomeMessage", "1.0.0") });


            var subscribers = subscriptionStorage.GetSubscriberAddressesForMessage(new[]
            {
                new MessageType("SomeMessage", "2.0.0")
            });

            Assert.AreEqual("subscriberA", subscribers.Single().Queue);
        }
    }
}