namespace NServiceBus.Gateway.Tests.Idempotency.Raven
{
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class When_acking_an_already_acked_message : in_the_raven_storage
    {
        [Test]
        public void The_ack_should_return_false_to_indicate_that_the_message_has_already_been_acked()
        {
            var message = CreateTestMessage();

            Store(message);

            byte[] msg;

            IDictionary<string, string> headers;
            ravenPersister.AckMessage(message.ClientId, out msg, out headers);

            Assert.False(ravenPersister.AckMessage(message.ClientId, out msg, out headers));
        }
    }
}