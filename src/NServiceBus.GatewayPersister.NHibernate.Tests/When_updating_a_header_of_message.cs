namespace NServiceBus.GatewayPersister.NHibernate.Tests
{
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class When_updating_a_header_of_message : BaseStorage
    {
        [Test]
        public void Should_update_the_header()
        {
            var message = CreateTestMessage();

            Store(message);

            var headerToUpdate = message.Headers.First();

            Persister.UpdateHeader(message.ClientId, headerToUpdate.Key, "Updated value");

            var updatedMessage = GetStoredMessage(message.ClientId);

            Assert.AreEqual(updatedMessage.Headers[headerToUpdate.Key], "Updated value");
        }
    }
}