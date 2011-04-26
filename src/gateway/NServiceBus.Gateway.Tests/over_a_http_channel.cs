namespace NServiceBus.Gateway.Tests
{
    using System;
    using System.Threading;
    using Channels;
    using Channels.Http;
    using Dispatchers;
    using Faults;
    using Notifications;
    using NUnit.Framework;
    using ObjectBuilder;
    using Persistence;
    using Rhino.Mocks;
    using Routing;
    using Unicast.Queuing;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport;

    public class over_a_http_channel:IHandleMessages<IMessage>
    {
        const string TEST_INPUT_QUEUE = "Gateway.Tests.Input";

        protected IChannelReceiver httpChannelReceiver;
        protected ISendMessages messageSender;

        
    
        [SetUp]
        public void SetUp()
        {
            httpChannelReceiver = new HttpChannelReceiver(new InMemoryPersistence());

            bus = Configure.With()
                .DefaultBuilder()
                .XmlSerializer()
                .FileShareDataBus("./databus_test_receiver")
                .InMemoryFaultManagement()
                .UnicastBus()
                .MsmqTransport()
                .CreateBus()
                .Start();

            var siteRegistry = MockRepository.GenerateStub<IRouteMessagesToSites>();
            var builder = MockRepository.GenerateStub<IBuilder>();
            var channelManager = MockRepository.GenerateStub<IManageChannels>();

            builder.Stub(x => x.Build(typeof(HttpChannelSender))).Return(new HttpChannelSender());
            builder.Stub(x => x.BuildAll<IRouteMessagesToSites>()).Return(new[] { siteRegistry });

            siteRegistry.Stub(x => x.GetDestinationSitesFor(Arg<TransportMessage>.Is.Anything)).Return(new[]{new Site
                                                                                                         {
                                                                                                             Address = "http://localhost:8090/Gateway/",
                                                                                                             ChannelType = typeof(HttpChannelSender),
                                                                                                             Key = "Not used"
                                                                                                         }});
            channelManager.Stub(x => x.GetDefaultChannel()).Return(new Channel
                                                                       {
                                                                           ReceiveAddress = "http://localhost:8092/Gateway/"
                                                                       });

            messageSender = MockRepository.GenerateStub<ISendMessages>();

            dispatcher = new TransactionalChannelDispatcher(builder,
                                                            channelManager,
                                                            MockRepository.GenerateStub<IMessageNotifier>(),
                                                            messageSender,
                                                            new FakeDistpatcherSettings());
            
            dispatcher.Start(TEST_INPUT_QUEUE);
        }
  
        protected IMessage GetResultingMessage()
        {
            messageReceived.WaitOne();
            return messageReturnedFromGateway;
        }
        protected IMessageContext GetResultingMessageContext()
        {
            messageReceived.WaitOne();
            return messageContext;
        }


        protected void SendHttpMessageToGateway(IMessage m)
        {
            messageReceived = new ManualResetEvent(false);

            bus.Send(TEST_INPUT_QUEUE, m);
        }

        public void Handle(IMessage m)
        {
            messageContext = bus.CurrentMessageContext;
            messageReturnedFromGateway = m;
            messageReceived.Set();
        }

        static IMessage messageReturnedFromGateway;
        static IMessageContext messageContext;
        static ManualResetEvent messageReceived;
        static IBus bus;
        TransactionalChannelDispatcher dispatcher;

    }

    public class FakeDistpatcherSettings : IMasterNodeSettings
    {
        public IReceiveMessages Receiver
        {
            get
            {
                return new MsmqMessageReceiver();
            }
        }

        public int NumberOfWorkerThreads
        {
            get { return 1; }
        }

        public int MaxRetries
        {
            get { return 1; }
        }

        public IManageMessageFailures FailureManager
        {
            get { return null; }
        }

        public string AddressOfAuditStore
        {
            get { return null; }
        }
    }
}