namespace NServiceBus.Gateway.Tests.HeaderManagement
{
    using System;
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
        const string idOfIncomingMessage = "xyz";

        [SetUp]
        public void SetUp()
        {
            addressOfOriginatingEndpoint = Address.Parse( "EndpointLocatedInSiteA");
        

            incomingMessage = new TransportMessage(Guid.NewGuid().ToString(),new Dictionary<string, string>(),addressOfOriginatingEndpoint);

            incomingMessage.Headers[Headers.OriginatingSite] = originatingSite;
            incomingMessage.Headers[Headers.HttpFrom] = originatingSite;
            gatewayHeaderManager = new GatewayHeaderManager();

            gatewayHeaderManager.MutateIncoming(incomingMessage);

            responseMessage = new TransportMessage
            {
                CorrelationId = idOfIncomingMessage
            };
        }
       
        [Test]
        public void Should_use_the_originating_siteKey_as_destination_for_response_messages()
        {      
            gatewayHeaderManager.MutateOutgoing(null, responseMessage);

            Assert.AreEqual(responseMessage.Headers[Headers.HttpTo], originatingSite);
        }

        [Test]
        public void Should_route_the_response_to_the_replyTo_address_specified_in_the_incoming_message()
        {
            gatewayHeaderManager.MutateOutgoing(null, responseMessage);

            Assert.AreEqual(Address.Parse(responseMessage.Headers[Headers.RouteTo]), addressOfOriginatingEndpoint);
        }
    }
}