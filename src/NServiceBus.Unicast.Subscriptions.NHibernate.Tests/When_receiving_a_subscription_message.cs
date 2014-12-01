using System.Transactions;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    [TestFixture]
    public class When_receiving_a_subscription_message : InMemoryDBFixture
    {
        [Test]
        public void A_subscription_entry_should_be_added_to_the_database()
        {
            var messageTypes = new[] { new MessageType(typeof(MessageA)), new MessageType(typeof(MessageB)) };
            
            using (var transaction = new TransactionScope())
            {
                storage.Subscribe(TestClients.ClientA, messageTypes);
                transaction.Complete();
            }

            using (var session = subscriptionStorageSessionProvider.OpenSession())
            {
                var subscriptions = session.CreateCriteria(typeof(Subscription)).List<Subscription>();

                Assert.AreEqual(subscriptions.Count, 2);
            }
        }

        [Test]
        public void Duplicate_subcription_shouldnt_create_additional_db_rows()
        {

            storage.Subscribe(TestClients.ClientA, MessageTypes.MessageA);
            storage.Subscribe(TestClients.ClientA, MessageTypes.MessageA);
            

            using (var session = subscriptionStorageSessionProvider.OpenSession())
            {
                var subscriptions = session.CreateCriteria(typeof(Subscription)).List<Subscription>();
                Assert.AreEqual(subscriptions.Count, 1);
            }
        }
    }
}
