namespace NServiceBus.Gateway.Tests.Idempotency.Raven
{
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class When_updating_a_header_of_message : in_the_raven_storage
    {
        [Test]
        public void Should_update_the_header()
        {
            var message = CreateTestMessage();

            Store(message);

            var headerToUpdate = message.Headers.First();

            ravenPersister.UpdateHeader(message.ClientId, headerToUpdate.Key,"Updated value");

            var updatedMessage = GetStoredMessage(message.ClientId);

            Assert.AreEqual(updatedMessage.Headers[headerToUpdate.Key], "Updated value");
        }
    }
}