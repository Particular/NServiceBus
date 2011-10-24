using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.Raven.Tests
{
    [TestFixture]
    [Ignore("Unsubscribe Is broken")]
    public class When_receiving_an_unsubscription_message : WithRavenSubscriptionStorage
    {
        [Test]
        public void All_subscription_entries_for_specfied_message_types_should_be_removed()
        {
            string clientEndpoint = "TestEndpoint";

            var messageTypes = new[] { new MessageType("MessageType1"), new MessageType("MessageType2") };

            //using (var transaction = new TransactionScope())
            //{
                storage.Subscribe(clientEndpoint, messageTypes);
               // transaction.Complete();
            //}


            //using (var transaction = new TransactionScope())
            //{
                storage.Unsubscribe(clientEndpoint, messageTypes);
            //    transaction.Complete();
            //}


            using (var session = store.OpenSession())
            {
                var subscriptions = session
                    .Query<Subscription>()
                    .Customize(c => c.WaitForNonStaleResults())
                    .Count();

                Assert.AreEqual(0, subscriptions);
            }
        }
    }
}