namespace NServiceBus.Gateway.Tests
{
    using NUnit.Framework;
    using Rhino.Mocks;
    using Unicast.Transport;

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

        [Test]
        public void Should_use_the_address_of_the_default_as_the_originating_site()
        {
            SendHttpMessageToGateway(new RegularMessage());

            var resultingMessage = GetResultingMessageContext();

            Assert.AreEqual(resultingMessage.Headers[Headers.OriginatingSite], "http://localhost:8092/Gateway/");
        }



        [Test,Ignore()]
        public void Should_enable_the_detionation_address_to_be_overriden_using_the_route_to_header()
        {
            var message = new RegularMessage();

            var destinationAddress = "EndpointA@someserver";

            message.SetHeader(Headers.RouteTo, destinationAddress);

            SendHttpMessageToGateway(message);

            var result = GetResultingMessage();

            messageSender.AssertWasCalled(
                x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<string>.Is.Equal(destinationAddress)));

        }
    }
}