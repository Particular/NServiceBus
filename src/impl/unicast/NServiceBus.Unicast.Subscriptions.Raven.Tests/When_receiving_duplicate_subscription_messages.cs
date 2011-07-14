using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.Raven.Tests
{
    [TestFixture]
    public class When_receiving_duplicate_subscription_messages : WithRavenSubscriptionStorage
    {
        [Test]
        public void shouldnt_create_additional_db_rows()
        {

            storage.Subscribe("testendpoint", new List<string> { "SomeMessageType" });
            storage.Subscribe("testendpoint", new List<string> { "SomeMessageType" });


            using (var session = store.OpenSession())
            {
                var subscriptions = session
                    .Query<Subscription>()
                    .Customize(c => c.WaitForNonStaleResults())
                    .Count();

                Assert.AreEqual(1, subscriptions);
            }
        }
    }
}