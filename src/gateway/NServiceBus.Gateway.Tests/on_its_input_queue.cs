namespace NServiceBus.Gateway.Tests
{
    using System.Threading;
    using Channels;
    using Channels.Http;
    using DataBus;
    using DataBus.FileShare;
    using Notifications;
    using NUnit.Framework;
    using Persistence;
    using Rhino.Mocks;
    using Unicast.Queuing;
    using Unicast.Transport;

    public class on_its_input_queue
    {
        protected ISendMessages testSender;
        protected IPersistMessages messagePersister;

        protected IChannelReceiver HttpChannelReceiver;
        IBus bus;

        protected const string DATABUS_DIRECTORY = "./databus_test_gateway";
        protected const string DATABUS_DIRECTORY_FOR_THE_TEST_ENDPOINT = "../../../databus.storage";
        const string GATEWAY_INPUT_QUEUE = "MasterEndpoint.Gateway";
        protected IDataBus dataBusForTheReceivingSide;

        [SetUp]
        public void SetUp()
        {
            testSender = MockRepository.GenerateStub<ISendMessages>();

            messagePersister = new InMemoryPersistence();
            dataBusForTheReceivingSide = new FileShareDataBus(DATABUS_DIRECTORY);

            HttpChannelReceiver = new HttpChannelReceiver(messagePersister)
                                      {
                                          ListenUrl = "http://localhost:8092/Gateway/",
                                          DataBus = dataBusForTheReceivingSide
                              };

            HttpChannelReceiver.MessageReceived += httpChannel_MessageReceived;

            HttpChannelReceiver.Start();


            bus = Configure.With()
                .DefaultBuilder()
                .XmlSerializer()
                .FileShareDataBus(DATABUS_DIRECTORY_FOR_THE_TEST_ENDPOINT)
                .InMemoryFaultManagement()
                .UnicastBus()
                .MsmqTransport()
                .CreateBus()
                .Start();
        }

        void httpChannel_MessageReceived(object sender, MessageReceivedOnChannelArgs e)
        {
            transportMessage = e.Message;

            messageReceived.Set();
        }

        TransportMessage transportMessage;
        ManualResetEvent messageReceived;

        protected TransportMessage GetResultingMessage()
        {
            messageReceived.WaitOne();
            return transportMessage;
        }


        protected void SendMessageToGatewayQueue(IMessage messageToSend)
        {
            transportMessage = null;
            messageReceived = new ManualResetEvent(false);

            bus.Send(GATEWAY_INPUT_QUEUE, messageToSend);
        }


        [TearDown]
        public void TearDown()
        {
            HttpChannelReceiver.Stop();
        }
    }
}