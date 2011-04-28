namespace NServiceBus.Gateway.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class When_a_message_is_sent : via_the_gateway
    {
      
        [Test]
        public void Should_send_the_message_to_the_specified_site()
        {
            SendMessage(HttpAddressForSiteB);

            var receivedMessage = GetReceivedMessage();

            Assert.NotNull(receivedMessage);
        }

        [Test]
        public void Should_set_the_return_address_to_the_gateway_itself()
        {
            SendMessage(HttpAddressForSiteB);

            var receivedMessage = GetReceivedMessage();

            Assert.AreEqual(receivedMessage.ReturnAddress,GatewayAddressForSiteB);
        }



        [Test]
        public void Should_use_the_address_of_the_default_channel_as_the_originating_site()
        {
            SendMessage(HttpAddressForSiteB);

            var receivedMessage = GetReceivedMessage();

            Assert.AreEqual(receivedMessage.Headers[Headers.OriginatingSite], HttpAddressForSiteA);
        }



        [Test,Ignore()]
        public void Should_enable_the_destination_address_to_be_overriden_using_the_route_to_header()
        {
           
            //const string destinationAddress = "EndpointA@someserver";

            //message.SetHeader(Headers.RouteTo, destinationAddress);

            //SendMessage(HttpAddressForSiteB,new[,]{newHeaders.RouteTo,destinationAddress});

            //var receivedMessage = GetReceivedMessage();

            //Assert.AreEqual(receivedMessage.ReturnAddress, destinationAddress);
        
        }

    }
}