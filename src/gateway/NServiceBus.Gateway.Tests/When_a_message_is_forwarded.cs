namespace NServiceBus.Gateway.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class When_a_message_is_forwarded : over_a_http_channel
    {
        [Test]
        public void Should_send_the_message_to_the_configured_endpoint()
        {
            SendHttpMessageToGateway(new RegularMessage());

            var resultingMessage = GetResultingMessage();
   
            Assert.NotNull(resultingMessage);
        }

        [Test]
        public void Should_set_the_return_address_to_the_gateway_itself()
        {
            SendHttpMessageToGateway(new RegularMessage());

            var resultingMessageContext = GetResultingMessageContext();

            Assert.True(resultingMessageContext.ReturnAddress.ToLower().StartsWith("masterendpoint.gateway"));
        }
    }
}