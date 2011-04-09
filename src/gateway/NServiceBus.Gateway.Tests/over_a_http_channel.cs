namespace NServiceBus.Gateway.Tests
{
    using System.Threading;
    using Channels;
    using Channels.Http;
    using Dispatchers;
    using Notifications;
    using NUnit.Framework;
    using Persistence;
    using Rhino.Mocks;

    public class over_a_http_channel:IHandleMessages<IMessage>
    {
        const string TEST_INPUT_QUEUE = "Gateway.Tests.Input";

        protected IChannelReceiver HttpChannelReceiver;
    
        [SetUp]
        public void SetUp()
        {
            HttpChannelReceiver = new HttpChannelReceiver(new InMemoryPersistence())
                              {
                                  ListenUrl = "http://localhost:8092/notused",
                                  ReturnAddress = "Gateway.Tests.Input"
                              };

            bus = Configure.With()
                .DefaultBuilder()
                .XmlSerializer()
                .FileShareDataBus("./databus_test_receiver")
                .InMemoryFaultManagement()
                .UnicastBus()
                .MsmqTransport()
                .CreateBus()
                .Start();

            
            dispatcher = new MsmqChannelDispatcher(new HttpChannelSender(), MockRepository.GenerateStub<IMessageNotifier>())
                             {
                                 InputQueue = TEST_INPUT_QUEUE,
                                 RemoteAddress = "http://localhost:8090/Gateway/"
                             };
          
            
            dispatcher.Start();
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

            m.SetHeader(Headers.RouteTo, "Gateway.Tests");
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
        MsmqChannelDispatcher dispatcher;

    }
}