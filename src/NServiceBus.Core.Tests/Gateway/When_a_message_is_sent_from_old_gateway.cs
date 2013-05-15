namespace NServiceBus.Gateway.Tests
{
    using NUnit.Framework;
    using Sending;

    public class When_a_message_is_sent_from_old_gateway : via_the_gateway
    {
        [Test]
        public void Should_send_the_message_to_the_specified_site()
        {
            Forwarder = () => idempotentForwarder;

            SendMessage(HttpAddressForSiteB);

            var receivedMessage = GetDetailsForReceivedMessage();

            Assert.NotNull(receivedMessage);
        }
    }
}
