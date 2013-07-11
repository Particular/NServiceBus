namespace NServiceBus.Gateway.Tests
{
    using NUnit.Framework;

    [TestFixture, Ignore("Need to redo all this tests because the gateway is now a satellite!")]
    public class When_a_message_is_sent : via_the_gateway
    {
        [Test]
        public void Should_send_the_message_to_the_specified_site()
        {
            SendMessage(HttpAddressForSiteB);

            var receivedMessage = GetDetailsForReceivedMessage();

            Assert.NotNull(receivedMessage);
        }

        [Test]
        public void Should_set_the_return_address_to_the_gateway_itself()
        {
            SendMessage(HttpAddressForSiteB);

            var receivedMessage = GetDetailsForReceivedMessage().Message;

            Assert.AreEqual(receivedMessage.ReplyToAddress, GatewayAddressForSiteB);
        }

        [Test]
        public void Should_set_the_default_channel_as_the_originating_site()
        {
            SendMessage(HttpAddressForSiteB);

            var receivedMessage = GetDetailsForReceivedMessage().Message;

            Assert.AreEqual(receivedMessage.Headers[Headers.OriginatingSite], defaultChannelForSiteA.ToString());
        }

        [Test]
        public void Should_route_the_message_to_the_receiving_gateways_main_input_queue()
        {
            SendMessage(HttpAddressForSiteB);

            Assert.AreEqual(GetDetailsForReceivedMessage().Destination, EndpointAddressForSiteB);
        }
    }
}