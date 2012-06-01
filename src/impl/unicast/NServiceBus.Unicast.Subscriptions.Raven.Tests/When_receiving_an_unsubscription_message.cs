using System.Linq;
using System.Transactions;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.Raven.Tests
{
    [TestFixture]
    public class When_receiving_an_unsubscription_message : WithRavenSubscriptionStorage
    {
        [Test]
        public void All_subscription_entries_for_specfied_message_types_should_be_removed()
        {
            using (var transaction = new TransactionScope())
            {
                storage.Subscribe(TestClients.ClientA, MessageTypes.All);
                transaction.Complete();
            }
            
            using (var transaction = new TransactionScope())
            {
                storage.Unsubscribe(TestClients.ClientA, MessageTypes.All);
                transaction.Complete();
            }

            var clients = storage.GetSubscriberAddressesForMessage(MessageTypes.All);
            Assert.IsFalse(clients.Any(a => a == TestClients.ClientA));
        }
    }
}