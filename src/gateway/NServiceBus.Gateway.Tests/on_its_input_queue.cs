namespace NServiceBus.Gateway.Tests
{
    using System.Threading;
    using NUnit.Framework;
    using Persistence;
    using Rhino.Mocks;
    using Unicast.Queuing;
    using Unicast.Transport;

    public class on_its_input_queue
    {
        protected ISendMessages testSender;
        protected IPersistMessages messagePersister;

        protected IChannel httpChannel;
        IBus bus;

        [SetUp]
        public void SetUp()
        {
            testSender = MockRepository.GenerateStub<ISendMessages>();

            messagePersister = new InMemoryPersistence();


            httpChannel = new HttpChannel(messagePersister)
                              {
                                  ListenUrl = "http://localhost:8092/Gateway/",
                                  ReturnAddress = "Gateway.Tests.Input"
                              };

            httpChannel.MessageReceived += httpChannel_MessageReceived;

            httpChannel.Start();


            bus = Configure.With()
                .DefaultBuilder()
                .XmlSerializer()
                .FileShareDataBus("./databus")
                .InMemoryFaultManagement()
                .UnicastBus()
                .MsmqTransport()
                .CreateBus()
                .Start();
        }

        void httpChannel_MessageReceived(object sender, MessageForwardingArgs e)
        {
            transportMessage = e.Message;

            messageReceived.Set();
        }

        TransportMessage transportMessage;
        ManualResetEvent messageReceived;

        protected object GetResultingMessage()
        {
            messageReceived.WaitOne();
            return transportMessage;
        }


        protected void SendMessageToGatewayQueue(IMessage messageToSend)
        {
            transportMessage = null;
            messageReceived = new ManualResetEvent(false);

            bus.Send("gateway", messageToSend);
        }


        [TearDown]
        public void TearDown()
        {
            httpChannel.Stop();
        }
    }
}