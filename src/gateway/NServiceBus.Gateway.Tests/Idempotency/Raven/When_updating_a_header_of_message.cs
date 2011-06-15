namespace NServiceBus.Gateway.Tests.Idempotency.Raven
{
    using System.Collections.Generic;
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



    [TestFixture]
    public class When_acking_a_existing_message : in_the_raven_storage
    {
        [Test]
        public void Should_return_the_message_and_headers()
        {
            var message = CreateTestMessage();

            Store(message);

            byte[] msg;

            IDictionary<string,string> headers;

            ravenPersister.AckMessage(message.ClientId,out msg,out headers);

            Assert.AreEqual(message.Headers,headers);
            Assert.AreEqual(message.OriginalMessage, msg);
        }
    }

}