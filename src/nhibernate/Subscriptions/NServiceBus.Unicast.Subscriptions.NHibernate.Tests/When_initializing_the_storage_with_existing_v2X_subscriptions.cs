namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class When_initializing_the_storage_with_existing_v2X_subscriptions : InMemoryDBFixture
    {
        [Test]
        public void Should_automatically_update_them_to_the_30_format()
        {
            using (var session = subscriptionStorageSessionProvider.OpenSession())
            {
                var command = session.Connection.CreateCommand();

                command.CommandText = string.Format("INSERT INTO Subscription([SubscriberEndpoint],[MessageType]) values ('{0}','{1}')", TestClients.ClientA, typeof(MessageB).AssemblyQualifiedName);

                command.ExecuteNonQuery();
            }

            storage.Init();

            var subscriptionsForMessageType = storage.GetSubscriberAddressesForMessage(MessageTypes.MessageB);

            Assert.AreEqual(1, subscriptionsForMessageType.Count());
            Assert.AreEqual(subscriptionsForMessageType.First(),TestClients.ClientA);
        }
    }
}