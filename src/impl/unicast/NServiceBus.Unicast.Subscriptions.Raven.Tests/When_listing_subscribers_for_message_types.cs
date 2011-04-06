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

            storage.Subscribe(clientEndpoint, new List<string> { "MessageType1" });
            storage.Subscribe(clientEndpoint, new List<string> { "MessageType2" });
            storage.Subscribe("some other endpoint", new List<string> { "MessageType1" });

            var subscriptionsForMessageType = storage.GetSubscribersForMessage(new List<String> { "MessageType1" });

            Assert.AreEqual(2, subscriptionsForMessageType.Count());
            Assert.AreEqual(clientEndpoint, subscriptionsForMessageType.First());
        }
    }
}