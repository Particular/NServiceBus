namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class When_using_semantic_versioning_of_messages : InMemoryDBFixture
    {
        [Test]
        public void Only_changes_in_major_version_should_effect_subscribers()
        {
            storage.Subscribe(TestClients.ClientA, MessageTypes.MessageA);
            storage.Subscribe(TestClients.ClientB, MessageTypes.MessageAv11);
            storage.Subscribe(TestClients.ClientC, MessageTypes.MessageAv2);

            Assert.AreEqual(2, storage.GetSubscriberAddressesForMessage(MessageTypes.MessageA).Count()); 
            Assert.AreEqual(2, storage.GetSubscriberAddressesForMessage(MessageTypes.MessageAv11).Count());
            Assert.AreEqual(1, storage.GetSubscriberAddressesForMessage(MessageTypes.MessageAv2).Count());
        }
    }
}