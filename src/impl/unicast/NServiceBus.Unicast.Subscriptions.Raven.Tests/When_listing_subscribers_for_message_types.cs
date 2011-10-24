using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.Raven.Tests
{
    [TestFixture]
    public class When_listing_subscribers_for_message_types : WithRavenSubscriptionStorage
    {
        [Test]
        public void The_names_of_all_subscibers_should_be_returned()
        {
            string clientEndpoint = "TestEndpoint";

            storage.Subscribe(new Address("testendpoint", "localhost"), new List<MessageType> { new MessageType("MessageType1") });
            storage.Subscribe(new Address("testendpoint", "localhost"), new List<MessageType> { new MessageType("MessageType2") });
            storage.Subscribe(new Address("otherendpoint", "localhost"), new List<MessageType> { new MessageType("MessageType1") });


            var subscriptionsForMessageType = storage.GetSubscriberAddressesForMessage(new [] { new MessageType("MessageType1") });

            Assert.AreEqual(2, subscriptionsForMessageType.Count());
            Assert.AreEqual(clientEndpoint, subscriptionsForMessageType.First());
        }
    }
}