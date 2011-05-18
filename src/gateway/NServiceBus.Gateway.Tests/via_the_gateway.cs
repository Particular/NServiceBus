﻿namespace NServiceBus.Gateway.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Channels;
    using Channels.Http;
    using DataBus;
    using Notifications;
    using NUnit.Framework;
    using ObjectBuilder;
    using Persistence;
    using Receiving;
    using Rhino.Mocks;
    using Routing;
    using Routing.Endpoints;
    using Routing.Sites;
    using Sending;
    using Unicast.Queuing;
    using Unicast.Transport;

    public class via_the_gateway
    {
        protected Address GatewayAddressForSiteA = Address.Parse("SiteAEndpoint.gateway@masternode_in_site_a");
        protected const string HttpAddressForSiteA = "http://localhost:8090/Gateway/";

        protected Address EndpointAddressForSiteB = Address.Parse("SiteBEndpoint@masternode_in_site_b");
        protected Address GatewayAddressForSiteB = Address.Parse("SiteBEndpoint.gateway@masternode_in_site_b");
        protected const string HttpAddressForSiteB = "http://localhost:8092/Gateway/";

        protected InMemoryDataBus databusForSiteA;
        protected InMemoryDataBus databusForSiteB;


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

            var channelManager = MockRepository.GenerateStub<IMangageReceiveChannels>();
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


            builder.Stub(x => x.Build<IdempotentSender>()).Return(new IdempotentSender(builder)
                                                                             {
                                                                                 DataBus = databusForSiteA
                                                                             });

            builder.Stub(x => x.Build<IReceiveMessagesFromSites>()).Return(new IdempotentReceiver(builder, new InMemoryPersistence()
                                                                                                               )
            {
                DataBus = databusForSiteB
            });
          
            builder.Stub(x => x.Build(typeof(HttpChannelReceiver))).Return(new HttpChannelReceiver());
            builder.Stub(x => x.Build(typeof(HttpChannelSender))).Return(new HttpChannelSender());

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
                                                                           Receiver = inMemoryReceiver
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
                                  TimeToBeReceived = TimeSpan.FromDays(1)
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