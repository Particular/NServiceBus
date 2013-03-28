namespace NServiceBus.Core.Tests.Persistence.RavenDB.SubscriptionStorage
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Transports.Msmq;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.Raven;

    [TestFixture]
    public class When_receiving_duplicate_subscription_messages : WithRavenSubscriptionStorage
    {
        [Test]
        public void shouldnt_create_additional_db_rows()
        {

            storage.Subscribe(new MsmqAddress("testendpoint", "localhost"), new List<MessageType> { new MessageType("SomeMessageType", "1.0.0.0") });
            storage.Subscribe(new MsmqAddress("testendpoint", "localhost"), new List<MessageType> { new MessageType("SomeMessageType", "1.0.0.0") });


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