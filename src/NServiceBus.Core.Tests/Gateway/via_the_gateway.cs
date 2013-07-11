namespace NServiceBus.Gateway.Tests
{
    using System;
    using System.Collections.Generic;
    using Channels;
    using Channels.Http;
    using Gateway.Routing;
    using Gateway.Routing.Endpoints;
    using Gateway.Routing.Sites;
    using NUnit.Framework;
    using Notifications;
    using ObjectBuilder;
    using Persistence;
    using Receiving;
    using Rhino.Mocks;
    using Sending;
    using Transports;
    using Unicast.Queuing;

    public class via_the_gateway
    {
        protected Address GatewayAddressForSiteA = Address.Parse("SiteAEndpoint.gateway@masternode_in_site_a");
        protected const string HttpAddressForSiteA = "http://localhost:8090/Gateway/";

        protected Address EndpointAddressForSiteB = Address.Parse("SiteBEndpoint@masternode_in_site_b");
        protected Address GatewayAddressForSiteB = Address.Parse("SiteBEndpoint.gateway@masternode_in_site_b");
        protected const string HttpAddressForSiteB = "http://localhost:8092/Gateway/";

        protected InMemoryDataBus databusForSiteA;
        protected InMemoryDataBus databusForSiteB;

        protected Channel defaultChannelForSiteA = new Channel {Address = HttpAddressForSiteA, Type = "http"};

        [TearDown]
        public void TearDown()
        {
            dispatcherInSiteA.Stop();
            receiverInSiteB.Stop();
        }

        [SetUp]
        public void SetUp()
        {
            databusForSiteA = new InMemoryDataBus();
            databusForSiteB = new InMemoryDataBus();
            fakeTransport = new FakeTransport();
         
            var builder = MockRepository.GenerateStub<IBuilder>();

            var channelFactory = new ChannelFactory();

            channelFactory.RegisterReceiver(typeof(HttpChannelReceiver));

            channelFactory.RegisterSender(typeof(HttpChannelSender));


            var channelManager = MockRepository.GenerateStub<IManageReceiveChannels>();
            channelManager.Stub(x => x.GetReceiveChannels()).Return(new[] {new ReceiveChannel()
                                                                              {
                                                                                  Address = HttpAddressForSiteB,
                                                                                  Type = "http",
                                                                                  NumberOfWorkerThreads = 1
                                                                              }});
            channelManager.Stub(x => x.GetDefaultChannel()).Return(defaultChannelForSiteA);

            builder.Stub(x => x.Build<IdempotentChannelForwarder>()).Return(new IdempotentChannelForwarder(channelFactory)
                                                                             {
                                                                                 DataBus = databusForSiteA
                                                                             });

            builder.Stub(x => x.Build<IReceiveMessagesFromSites>()).Return(new IdempotentChannelReceiver(channelFactory, new InMemoryPersistence())
            {
                DataBus = databusForSiteB
            });

            builder.Stub(x => x.BuildAll<IRouteMessagesToSites>()).Return(new[] { new KeyPrefixConventionSiteRouter() });

            messageSender = new FakeMessageSender();
            receiverInSiteB = new GatewayReceiver();
            receiverInSiteB.ChannelManager = channelManager;
            receiverInSiteB.EndpointRouter = new DefaultEndpointRouter
                {
                    MainInputAddress = EndpointAddressForSiteB
                };
            receiverInSiteB.MessageSender = messageSender;
            receiverInSiteB.builder = builder;
            //receiverInSiteB.InputAddress = GatewayAddressForSiteA;

            dispatcherInSiteA = new GatewaySender();
            dispatcherInSiteA.ChannelManager = channelManager;
            dispatcherInSiteA.Builder = builder;
            dispatcherInSiteA.MessageSender = MockRepository.GenerateStub<ISendMessages>();
            dispatcherInSiteA.Notifier = MockRepository.GenerateStub<IMessageNotifier>();
           // dispatcherInSiteA.InputAddress = GatewayAddressForSiteA;

            dispatcherInSiteA.Start();
            receiverInSiteB.Start();
        }

        protected void SendMessage(string destinationSites)
        {
            SendMessage(destinationSites, new Dictionary<string, string>());
        }

        protected void SendMessage(string destinationSites,Dictionary<string,string> headers)
        {
            var message = new TransportMessage(Guid.NewGuid().ToString(),headers)
                              {
                                  Body = new byte[500],
                                  TimeToBeReceived = TimeSpan.FromDays(1),
                                  ReplyToAddress = GatewayAddressForSiteA
                              };

            message.Headers[Headers.DestinationSites] = destinationSites;
          
            SendMessage(message);
        }

        protected void SendMessage(TransportMessage message)
        {
            fakeTransport.RaiseEvent(message);
        }

        protected FakeMessageSender.SendDetails GetDetailsForReceivedMessage()
        {
            return messageSender.GetResultingMessage();
        }

        GatewaySender dispatcherInSiteA;
        GatewayReceiver receiverInSiteB;
        FakeTransport fakeTransport;
        FakeMessageSender messageSender;
    }
}