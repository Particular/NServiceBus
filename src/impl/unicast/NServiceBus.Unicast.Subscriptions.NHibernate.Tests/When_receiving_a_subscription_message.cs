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
            string clientEndpoint = "TestEndpoint";

            var messageTypes = new List<String> { "MessageType1", "MessageType2" };

            using (var transaction = new TransactionScope())
            {
                storage.Subscribe(clientEndpoint, messageTypes);
                transaction.Complete();
            }

            using (var session = sessionSource.CreateSession())
            {
                var subscriptions = session.CreateCriteria(typeof(Subscription)).List<Subscription>();

                Assert.AreEqual(subscriptions.Count, 2);
            }
        }

        [Test]
        public void Duplicate_subcriptions_shouldnt_create_aditional_db_rows()
        {

            storage.Subscribe("testendpoint", new List<string> { "SomeMessageType" });
            storage.Subscribe("testendpoint", new List<string> { "SomeMessageType" });


            using (var session = sessionSource.CreateSession())
            {
                var subscriptions = session.CreateCriteria(typeof(Subscription)).List<Subscription>();
                Assert.AreEqual(subscriptions.Count, 1);
            }
        }
    }
}
