using System.Transactions;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    [TestFixture]
    public class When_receiving_a_unsubscription_message : InMemoryDBFixture
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


            using (var session = subscriptionStorageSessionProvider.OpenSession())
            {
                var subscriptions = session.CreateCriteria(typeof(Subscription)).List<Subscription>();
                Assert.AreEqual(subscriptions.Count, 0);
            }
        }
    }
}