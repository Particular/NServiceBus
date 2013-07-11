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
            storage.Subscribe(TestClients.ClientA, MessageTypes.MessageA);
            storage.Subscribe(TestClients.ClientA, MessageTypes.MessageB);
            storage.Subscribe(TestClients.ClientB, MessageTypes.MessageA);
            storage.Subscribe(TestClients.ClientA, MessageTypes.MessageAv2);

            var subscriptionsForMessageType = storage.GetSubscriberAddressesForMessage(MessageTypes.MessageA);

            Assert.AreEqual(2,subscriptionsForMessageType.Count());
            Assert.AreEqual(TestClients.ClientA, subscriptionsForMessageType.First());
        }

        [Test]
        public void Duplicates_should_not_be_generated_for_interface_inheritance_chains()
        {
            storage.Subscribe(TestClients.ClientA, new[] { new MessageType(typeof(ISomeInterface)) });
            storage.Subscribe(TestClients.ClientA, new[] { new MessageType(typeof(ISomeInterface2)) });
            storage.Subscribe(TestClients.ClientA, new[] { new MessageType(typeof(ISomeInterface3)) });
            
            var subscriptionsForMessageType = storage.GetSubscriberAddressesForMessage(new[] {  new MessageType(typeof(ISomeInterface)), new MessageType(typeof(ISomeInterface2)), new MessageType(typeof(ISomeInterface3)) });

            Assert.AreEqual(1,subscriptionsForMessageType.Count());
        }
    }
}