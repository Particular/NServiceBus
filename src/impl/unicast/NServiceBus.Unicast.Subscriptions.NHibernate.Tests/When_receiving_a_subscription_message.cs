using System;
using System.Collections.Generic;
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
            var clientEndpoint = Address.Parse("TestEndpoint");

            var messageTypes = new List<String> { "MessageType1", "MessageType2" };

            using (var transaction = new TransactionScope())
            {
                storage.Subscribe(clientEndpoint, messageTypes);
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

            storage.Subscribe(Address.Parse("testendpoint"), new List<string> { "SomeMessageType" });
            storage.Subscribe(Address.Parse("testendpoint"), new List<string> { "SomeMessageType" });


            using (var session = subscriptionStorageSessionProvider.OpenSession())
            {
                var subscriptions = session.CreateCriteria(typeof(Subscription)).List<Subscription>();
                Assert.AreEqual(subscriptions.Count, 1);
            }
        }
    }
}
