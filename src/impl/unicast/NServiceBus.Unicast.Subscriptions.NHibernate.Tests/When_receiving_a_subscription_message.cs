using System;
using System.Collections.Generic;
using System.Transactions;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    [TestFixture]
    public class When_receiving_a_subscription_message : InMemoryDBFixture
    {
        private ISubscriptionStorage storage;
        protected override void Before_each_test()
        {
            base.Before_each_test();

            storage = new SubscriptionStorage(sessionSource);
        }

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

            var subscriptions = session.CreateCriteria(typeof(Subscription)).List<Subscription>();

            Assert.AreEqual(subscriptions.Count, 2);

        }

        [Test]
        public void Duplicate_subcription_shouldnt_create_aditional_db_rows()
        {

            using (var transaction = new TransactionScope())
            {
                storage.Subscribe("testendpoint", new List<string> { "SomeMessageType" });
                storage.Subscribe("testendpoint", new List<string> { "SomeMessageType" });

                transaction.Complete();
            }
            var subscriptions = session.CreateCriteria(typeof(Subscription)).List<Subscription>();
            Assert.AreEqual(subscriptions.Count, 1);

        }
    }
}
