using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.Raven.Tests
{
    [TestFixture]
    public class When_receiving_duplicate_subscription_messages : WithRavenSubscriptionStorage
    {
        [Test]
        public void shouldnt_create_additional_db_rows()
        {

            storage.Subscribe(new Address("testendpoint", "localhost"), new List<MessageType> { new MessageType("SomeMessageType","1.0.0.0") });
            storage.Subscribe(new Address("testendpoint", "localhost"), new List<MessageType> { new MessageType("SomeMessageType", "1.0.0.0") });


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