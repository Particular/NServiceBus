using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    [TestFixture]
    public class When_listing_subscribers_for_message_types : InMemoryDBFixture
    {
        [Test]
        public void The_names_of_all_subscibers_should_be_returned()
        {
            string clientEndpoint = "TestEndpoint";

            storage.Subscribe(clientEndpoint, new List<string> { "MessageType1" });
            storage.Subscribe(clientEndpoint, new List<string> { "MessageType2" });
            storage.Subscribe("some other endpoint", new List<string> { "MessageType1" });

            var subscriptionsForMessageType = storage.GetSubscribersForMessage(new List<String> { "MessageType1" });

            Assert.AreEqual(subscriptionsForMessageType.Count, 2);
            Assert.AreEqual(subscriptionsForMessageType[0], clientEndpoint);
        }
    }
}