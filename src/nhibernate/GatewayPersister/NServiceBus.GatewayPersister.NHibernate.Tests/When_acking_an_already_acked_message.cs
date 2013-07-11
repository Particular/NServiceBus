namespace NServiceBus.GatewayPersister.NHibernate.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class When_acking_an_already_acked_message : BaseStorage
    {
        [Test]
        public void The_ack_should_return_false_to_indicate_that_the_message_has_already_been_acked()
        {
            var message = CreateTestMessage();

            Store(message);

            byte[] msg;

            IDictionary<string, string> headers;
            Persister.AckMessage(message.ClientId, out msg, out headers);

            Assert.False(Persister.AckMessage(message.ClientId, out msg, out headers));
        }
    }
}