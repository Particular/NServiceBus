using System.Collections.Generic;
using System.Transactions;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    [TestFixture]
    public class When_receiving_a_unsubscription_message :InMemoryDBFixture
    {
        private ISubscriptionStorage storage;
        protected override void Before_each_test()
        {
            base.Before_each_test();

            storage = new SubscriptionStorage(sessionSource);
        }
      
        [Test]
        public void All_subscription_entries_for_specfied_message_types_should_be_removed()
        {
            string clientEndpoint = "TestEndpoint";

            var messageTypes = new List<string> { "MessageType1", "MessageType2" };

            using (var transaction = new TransactionScope())
            {
                storage.Subscribe(clientEndpoint, messageTypes);
                transaction.Complete();
            }


            using (var transaction = new TransactionScope())
            {
                storage.Unsubscribe(clientEndpoint, messageTypes);
                transaction.Complete();
            }


            var subscriptions = session.CreateCriteria(typeof(Subscription)).List<Subscription>();

            Assert.AreEqual(subscriptions.Count, 0);
        }
    }
}