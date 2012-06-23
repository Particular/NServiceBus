using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using NServiceBus.Unicast.Transport;
using NUnit.Framework;

namespace NServiceBus.Unicast.Queuing.Azure.Tests
{
    public abstract class AzureQueueFixture
    {
        protected AzureMessageQueueSender sender;
        protected AzureMessageQueueReceiver receiver;
        protected CloudQueueClient client;
        protected CloudQueue nativeQueue;


        protected virtual string QueueName
        {
            get
            {
                return "testqueue";
            }
        }

        protected virtual bool PurgeOnStartup { get{ return false;} }

        [SetUp]
        public void Setup()
        {
            client = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudQueueClient();
       
            nativeQueue = client.GetQueueReference(QueueName);

            nativeQueue.CreateIfNotExist();
            nativeQueue.Clear();


            sender = new AzureMessageQueueSender
                        {
                            Client = client
                        };

            sender.Init(QueueName, true);

            receiver = new AzureMessageQueueReceiver
            {
                Client = client
            };

            sender.Init(QueueName, true);
        }

        protected void AddTestMessage()
        {
            AddTestMessage(new TransportMessage());
        }

        protected void AddTestMessage(TransportMessage messageToAdd)
        {
            sender.Send(messageToAdd, QueueName);
        }

    }
}