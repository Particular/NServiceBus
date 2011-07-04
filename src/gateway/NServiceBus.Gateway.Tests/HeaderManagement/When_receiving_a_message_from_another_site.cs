namespace NServiceBus.Gateway.Tests.HeaderManagement
{
    using System;
    using System.Collections.Generic;
    using MessageHeaders;
    using NUnit.Framework;
    using Gateway.HeaderManagement;
    using Rhino.Mocks;
    using Unicast;
    using Unicast.Transport;

    [TestFixture]
    public class When_receiving_a_message_from_another_site
    {
        GatewayHeaderManager gatewayHeaderManager;
        IMessage incomingMessage;
        TransportMessage responseMessage;

        MessageHeaderManager headerManager;
        Address addressOfOriginatingEndpoint;
        const string originatingSite = "SiteA";
        const string idOfIncommingMessage = "xyz";

        [SetUp]
        public void SetUp()
        {
            var bus = MockRepository.GenerateStub<IBus>();
            addressOfOriginatingEndpoint = Address.Parse( "EnpointLocatedInSiteA");
        
            headerManager = new MessageHeaderManager();

            bus.Stub(x => x.CurrentMessageContext).Return(new FakeMessageContext
                                                              {
                                                                  Id = idOfIncommingMessage,
                                                                  ReplyToAddress = addressOfOriginatingEndpoint
                                                              });
            ExtensionMethods.SetHeaderAction = headerManager.SetHeader;
            ExtensionMethods.GetHeaderAction = headerManager.GetHeader;

            incomingMessage = new TestMessage();

            incomingMessage.SetOriginatingSiteHeader(originatingSite);

            gatewayHeaderManager = new GatewayHeaderManager
                                       {
                                           Bus = bus
                                       };

            gatewayHeaderManager.MutateIncoming(incomingMessage);

            responseMessage = new TransportMessage
            {
                Headers = new Dictionary<string, string>(),
                CorrelationId = idOfIncommingMessage
            };

        }
       
        [Test]
        public void Should_use_the_originating_sitekey_as_destination_for_response_messages()
        {      
            gatewayHeaderManager.MutateOutgoing(null, responseMessage);

            Assert.AreEqual(responseMessage.Headers[Headers.DestinationSites],originatingSite);
        }

        [Test]
        public void Should_route_the_response_to_the_replyto_address_specified_in_the_incoming_message()
        {
            gatewayHeaderManager.MutateOutgoing(null, responseMessage);

            Assert.AreEqual(Address.Parse(responseMessage.Headers[Headers.RouteTo]), addressOfOriginatingEndpoint);
        }
    }

    public class FakeMessageContext : IMessageContext
    {
        public string Id { get;  set; }
        public string ReturnAddress 
        {
            get { return ReplyToAddress.ToString(); }
            set { ReplyToAddress = Address.Parse(value); }
        }

        public Address ReplyToAddress { get; set; }

        public DateTime TimeSent { get;  set; }
        public IDictionary<string, string> Headers { get;  set; }
    }

    public class TestMessage : IMessage
    {
    }
}