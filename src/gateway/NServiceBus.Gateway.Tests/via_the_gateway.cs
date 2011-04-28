namespace NServiceBus.Gateway.Tests
{
    using System;
    using System.Collections.Generic;
    using Channels;
    using Channels.Http;
    using Notifications;
    using NUnit.Framework;
    using ObjectBuilder;
    using Persistence;
    using Rhino.Mocks;
    using Routing;
    using Routing.Endpoints;
    using Routing.Sites;
    using Unicast.Queuing;
    using Unicast.Transport;

    public class via_the_gateway
    {
        protected const string GatewayAddressForSiteA = "SiteAEndpoint.gateway@masternode_in_site_a";
        protected const string HttpAddressForSiteA = "http://localhost:8090/Gateway/";

        protected const string GatewayAddressForSiteB = "SiteBEndpoint.gateway@masternode_in_site_b";
        protected const string HttpAddressForSiteB = "http://localhost:8092/Gateway/";

        InputDispatcher dispatcherInSiteA;
        TransactionalReceiver receiverInSiteB;

        [TearDown]
        public void TearDown()
        {
            dispatcherInSiteA.Dispose();
            receiverInSiteB.Dispose();
        }
        [SetUp]
        public void SetUp()
        {
            inMemoryReceiver = new InMemoryReceiver();
         
            var builder = MockRepository.GenerateStub<IBuilder>();
            var channelManager = MockRepository.GenerateStub<IManageChannels>();
            channelManager.Stub(x => x.GetActiveChannels()).Return(new[] {new Channel
                                                                              {
                                                                                  NumWorkerThreads = 1,
                                                                                  ReceiveAddress = HttpAddressForSiteB,
                                                                                  Receiver = typeof(HttpChannelReceiver)
                                                                              }});
            channelManager.Stub(x => x.GetDefaultChannel()).Return(new Channel
                                                                       {
                                                                           NumWorkerThreads = 1,
                                                                           ReceiveAddress = HttpAddressForSiteA,
                                                                           Receiver = typeof(HttpChannelReceiver)
                                                                       });



            builder.Stub(x => x.Build(typeof(HttpChannelReceiver))).Return(new HttpChannelReceiver(new InMemoryPersistence()));
            builder.Stub(x => x.Build(typeof(HttpChannelSender))).Return(new HttpChannelSender());
            builder.Stub(x => x.BuildAll<IRouteMessagesToSites>()).Return(new[] { new KeyPrefixConventionSiteRouter() });

            messageSender = new FakeMessageSender();
            receiverInSiteB = new TransactionalReceiver(channelManager,new LegacyEndpointRouter(),builder,messageSender);
           
            dispatcherInSiteA = new InputDispatcher(builder,
                                                                   channelManager,
                                                                   MockRepository.GenerateStub<IMessageNotifier>(),
                                                                   MockRepository.GenerateStub<ISendMessages>(),
                                                                   new FakeDistpatcherSettings
                                                                       {
                                                                           Receiver = inMemoryReceiver
                                                                       });

            dispatcherInSiteA.Start(GatewayAddressForSiteA);
            receiverInSiteB.Start(GatewayAddressForSiteB);
        }



        protected void SendMessage(string destinationSites)
        {
            var message = new TransportMessage
                              {
                                  Id =  Guid.NewGuid().ToString(),
                                  Headers = new Dictionary<string, string>(),
                                  Body = new byte[1]
                              };

            message.Headers[Headers.DestinationSites] = destinationSites;
          
            SendMessage(message);
        }

        protected void SendMessage(TransportMessage message)
        {
            inMemoryReceiver.Add(message);
        }

        protected TransportMessage GetReceivedMessage()
        {
            return messageSender.GetResultingMessage();
        }


 
        InMemoryReceiver inMemoryReceiver;
        FakeMessageSender messageSender;
     
    }
}