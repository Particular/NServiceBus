namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using MasterNode;
    using MessageMutator;
    using NUnit.Framework;
    using ObjectBuilder;
    using Queuing;
    using Rhino.Mocks;
    using Serialization;
    using SomeUserNamespace;
    using Transport;

    [TestFixture]
    public class When_sending_messages_to_sites
    {
        IBus bus;
        ISendMessages messageSender;
        string gatewayAddress;
        MessageHeaderManager headerManager = new MessageHeaderManager();

        [SetUp]
        public void SetUp()
        {
            string masterNodeAddress = "MasterNode";
            string localAddress = "endpointA";
            gatewayAddress = localAddress + ".gateway@" + masterNodeAddress;

            try
            {
                Address.InitializeLocalAddress(localAddress);
            }
            catch // intentional
            {
            }

            messageSender = MockRepository.GenerateStub<ISendMessages>();
            var masterNodeManager = MockRepository.GenerateStub<IManageTheMasterNode>();
            var builder = MockRepository.GenerateStub<IBuilder>();

            builder.Stub(x => x.BuildAll<IMutateOutgoingMessages>()).Return(new IMutateOutgoingMessages[] { });

            builder.Stub(x => x.BuildAll<IMutateOutgoingTransportMessages>()).Return(new IMutateOutgoingTransportMessages[] { headerManager});

            masterNodeManager.Stub(x => x.GetMasterNode()).Return(masterNodeAddress);
            bus = new UnicastBus
                      {
                          MessageSerializer = MockRepository.GenerateStub<IMessageSerializer>(),
                          Builder = builder,
                          MasterNodeManager = masterNodeManager,
                          MessageSender = messageSender,
                          Transport = MockRepository.GenerateStub<ITransport>()
                      };

            ExtensionMethods.SetHeaderAction = headerManager.SetHeader;

            ((IStartableBus)bus).Start();
        }


        [Test]
        public void The_destination_sites_header_should_be_set_to_the_given_sitekeys()
        {
            bus.SendToSites(new[] { "SiteA,SiteB" }, new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers.ContainsKey(Headers.DestinationSites)), Arg<string>.Is.Anything));
        }

        [Test]
        public void The_gateway_address_should_be_generated_based_on_the_master_node()
        {
            bus.SendToSites(new[] { "SiteA,SiteB" }, new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<string>.Is.Equal(gatewayAddress)));
        }




    }


    public class MessageHeaderManager : IMutateOutgoingTransportMessages
    {
        void IMutateOutgoingTransportMessages.MutateOutgoing(IMessage[] messages, TransportMessage transportMessage)
        {
            if (messageHeaders != null)
                if (messageHeaders.ContainsKey(messages[0]))
                    foreach (var key in messageHeaders[messages[0]].Keys)
                        transportMessage.Headers.Add(key, messageHeaders[messages[0]][key]);
        }

      
        public void SetHeader(IMessage message, string key, string value)
        {
            if (message == ExtensionMethods.CurrentMessageBeingHandled)
                throw new InvalidOperationException("Cannot change headers on the message being processed.");

            if (messageHeaders == null)
                messageHeaders = new Dictionary<IMessage, IDictionary<string, string>>();

            if (!messageHeaders.ContainsKey(message))
                messageHeaders.Add(message, new Dictionary<string, string>());

            if (!messageHeaders[message].ContainsKey(key))
                messageHeaders[message].Add(key, value);
            else
                messageHeaders[message][key] = value;
        }

        private static IDictionary<IMessage, IDictionary<string, string>> messageHeaders;
    }
}