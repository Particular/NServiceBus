using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    [TestFixture]
    public class When_listing_subscribers_for_message_types : InMemoryDBFixture
    {
        [Test]
        public void The_names_of_all_subscribers_should_be_returned()
        {
            var clientEndpoint = Address.Parse("TestEndpoint");

            storage.Subscribe(clientEndpoint, new List<string> { "MessageType1" });
            storage.Subscribe(clientEndpoint, new List<string> { "MessageType2" });
            storage.Subscribe(Address.Parse("some other endpoint"), new List<string> { "MessageType1" });

            var subscriptionsForMessageType = storage.GetSubscriberAddressesForMessage(new List<String> { "MessageType1" });

            Assert.AreEqual(2,subscriptionsForMessageType.Count());
            Assert.AreEqual(clientEndpoint,subscriptionsForMessageType.First());
        }

        [Test]
        public void Duplicates_should_not_be_generated_for_interface_inheritance_chains()
        {
            var clientEndpoint = Address.Parse("TestEndpoint");

            //ISomeInterface3:ISomeInterface2:ISomeInterface
            storage.Subscribe(clientEndpoint, new List<string> { "ISomeInterface" });
            storage.Subscribe(clientEndpoint, new List<string> { "ISomeInterface2" });
            storage.Subscribe(clientEndpoint, new List<string> { "ISomeInterface3" });

            var subscriptionsForMessageType = storage.GetSubscriberAddressesForMessage(new List<String> { "ISomeInterface", "ISomeInterface2", "ISomeInterface3" });

            Assert.AreEqual(1,subscriptionsForMessageType.Count());
        }
    }
}