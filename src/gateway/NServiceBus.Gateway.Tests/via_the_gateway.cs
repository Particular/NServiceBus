namespace NServiceBus.Gateway.Tests
{
    using System;
    using System.Collections.Generic;
    using Channels;
    using Channels.Http;
    using Faults;
    using Gateway.Routing;
    using Gateway.Routing.Endpoints;
    using Gateway.Routing.Sites;
    using Notifications;
    using NUnit.Framework;
    using ObjectBuilder;
    using Persistence;
    using Receiving;
    using Rhino.Mocks;
    using Sending;
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
            dispatcherInSiteA.Dispose();
            receiverInSiteB.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            databusForSiteA = new InMemoryDataBus();
            databusForSiteB = new InMemoryDataBus();

            inMemoryReceiver = new InMemoryReceiver();

            var builder = MockRepository.GenerateStub<IBuilder>();

            var channelFactory = new ChannelFactory();

            channelFactory.RegisterReceiver(typeof(HttpChannelReceiver));

            channelFactory.RegisterSender(typeof(HttpChannelSender));


            var channelManager = MockRepository.GenerateStub<IMangageReceiveChannels>();
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
            receiverInSiteB = new GatewayReceiver(channelManager,new DefaultEndpointRouter
                                                                     {
                                                                         MainInputAddress = EndpointAddressForSiteB
                                                                     },builder,messageSender);
           
            dispatcherInSiteA = new GatewaySender(builder,
                                                                   channelManager,
                                                                   MockRepository.GenerateStub<IMessageNotifier>(),
                                                                   MockRepository.GenerateStub<ISendMessages>(),
                                                                   new FakeDispatcherSettings
                                                                       {
                                                                           Receiver = inMemoryReceiver,
                                                                           FailureManager = MockRepository.GenerateStub<IManageMessageFailures>()
                                                                       });

            dispatcherInSiteA.Start(GatewayAddressForSiteA);
            receiverInSiteB.Start(GatewayAddressForSiteB);
        }

        protected void SendMessage(string destinationSites)
        {
            SendMessage(destinationSites, new Dictionary<string, string>());
        }

        protected void SendMessage(string destinationSites,Dictionary<string,string> headers)
        {
            var message = new TransportMessage
                              {
                                  Id =  Guid.NewGuid().ToString(),
                                  Headers = headers,
                                  Body = new byte[500],
                                  TimeToBeReceived = TimeSpan.FromDays(1),
                                  ReplyToAddress = GatewayAddressForSiteA
                              };

            message.Headers[Headers.DestinationSites] = destinationSites;
          
            SendMessage(message);
        }

        protected void SendMessage(TransportMessage message)
        {
            inMemoryReceiver.Add(message);
        }

        protected FakeMessageSender.SendDetails GetDetailsForReceivedMessage()
        {
            return messageSender.GetResultingMessage();
        }

        GatewaySender dispatcherInSiteA;
        GatewayReceiver receiverInSiteB;
        InMemoryReceiver inMemoryReceiver;
        FakeMessageSender messageSender;
    }
}