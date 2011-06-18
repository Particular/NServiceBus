namespace NServiceBus.Gateway.Tests.Idempotency.Raven
{
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class When_acking_an_existing_message : in_the_raven_storage
    {
        [Test]
        public void Should_return_the_message_and_headers()
        {
            var message = CreateTestMessage();

            Store(message);

            byte[] msg;

            IDictionary<string, string> headers;

            ravenPersister.AckMessage(message.ClientId, out msg, out headers);

            Assert.AreEqual(message.Headers, headers);
            Assert.AreEqual(message.OriginalMessage, msg);
        }
    }
}