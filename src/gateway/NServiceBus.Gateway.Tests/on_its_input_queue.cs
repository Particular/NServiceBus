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

        protected IChannelReceiver httpChannelReceiver;
        IBus bus;
        protected const string DATABUS_DIRECTORY = "./databus_test_gateway";
        protected const string DATABUS_DIRECTORY_FOR_THE_TEST_ENDPOINT = "../../../databus.storage";
        const string LISTEN_URL = "http://localhost:8092/Gateway/";
        protected IDataBus dataBusForTheReceivingSide;

        [SetUp]
        public void SetUp()
        {
            testSender = MockRepository.GenerateStub<ISendMessages>();

            messagePersister = new InMemoryPersistence();
            dataBusForTheReceivingSide = new FileShareDataBus(DATABUS_DIRECTORY);

            httpChannelReceiver = new HttpChannelReceiver(messagePersister)

                                      {
                                          DataBus = dataBusForTheReceivingSide
                              };

            httpChannelReceiver.MessageReceived += httpChannel_MessageReceived;

            httpChannelReceiver.Start(LISTEN_URL,1);

            //todo - this needs to be refactored when the gateway is merged into the nsb.core
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


            //todo the master node manager that reads from config doesn't throw when no config section is found? - should be an exception
            //todo the bus isn't handling the servername correctly, disuss this with Udi - MasterNode == servername
            bus.SendToSites(new[] { LISTEN_URL }, messageToSend);
        }


        [TearDown]
        public void TearDown()
        {
            httpChannelReceiver.Stop();
        }
    }
}