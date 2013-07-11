namespace NServiceBus.Gateway.Tests.HeaderManagement
{
    using System.Collections.Generic;
    using Gateway.HeaderManagement;
    using NUnit.Framework;

    [TestFixture]
    public class When_receiving_a_message_from_another_site
    {
        GatewayHeaderManager gatewayHeaderManager;
        TransportMessage incomingMessage;
        TransportMessage responseMessage;

        Address addressOfOriginatingEndpoint;
        const string originatingSite = "SiteA";
        const string idOfIncommingMessage = "xyz";

        [SetUp]
        public void SetUp()
        {
            addressOfOriginatingEndpoint = Address.Parse( "EnpointLocatedInSiteA");
        

            incomingMessage = new TransportMessage
            {
                ReplyToAddress = addressOfOriginatingEndpoint
            };

            incomingMessage.Headers[Headers.OriginatingSite] = originatingSite;
            incomingMessage.Headers[Headers.HttpFrom] = originatingSite;
            gatewayHeaderManager = new GatewayHeaderManager();

            gatewayHeaderManager.MutateIncoming(incomingMessage);

            responseMessage = new TransportMessage
            {
                CorrelationId = idOfIncommingMessage
            };
        }
       
        [Test]
        public void Should_use_the_originating_sitekey_as_destination_for_response_messages()
        {      
            gatewayHeaderManager.MutateOutgoing(null, responseMessage);

            Assert.AreEqual(responseMessage.Headers[Headers.HttpTo], originatingSite);
        }

        [Test]
        public void Should_route_the_response_to_the_replyto_address_specified_in_the_incoming_message()
        {
            gatewayHeaderManager.MutateOutgoing(null, responseMessage);

            Assert.AreEqual(Address.Parse(responseMessage.Headers[Headers.RouteTo]), addressOfOriginatingEndpoint);
        }
    }
}