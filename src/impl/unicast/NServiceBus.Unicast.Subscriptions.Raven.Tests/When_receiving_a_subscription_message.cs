using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.Raven.Tests
{
    using NLog.LayoutRenderers;
    using Newtonsoft.Json;

    [TestFixture]
    public class When_receiving_a_subscription_message : WithRavenSubscriptionStorage
    {
        [Test]
        public void A_subscription_entry_should_be_added_to_the_database()
        {
            string clientEndpoint = "TestEndpoint";

            var messageTypes = new[] { new MessageType("MessageType1"), new MessageType("MessageType2") };

            using (var transaction = new TransactionScope())
            {
                storage.Subscribe(clientEndpoint, messageTypes);
                transaction.Complete();
            }

            using (var session = store.OpenSession())
            {
                var subscriptions = session
                    .Query<Subscription>()
                    .Customize(c => c.WaitForNonStaleResults())
                    .Count();

                Assert.AreEqual(2, subscriptions);
            }
        }
    }
}
