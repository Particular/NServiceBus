namespace NServiceBus.Core.Tests.Persistence.RavenDB.SubscriptionStorage
{
    using System.Linq;
    using System.Transactions;
    using NServiceBus.Persistence.Raven.SubscriptionStorage;
    using NUnit.Framework;
    using Unicast.Subscriptions;

    [TestFixture]
    public class When_receiving_a_subscription_message : WithRavenSubscriptionStorage
    {
        [Test]
        public void A_subscription_entry_should_be_added_to_the_database()
        {
            Address clientEndpoint = Address.Parse("TestEndpoint");

            var messageTypes = new[] { new MessageType("MessageType1", "1.0.0.0"), new MessageType("MessageType2", "1.0.0.0") };

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
