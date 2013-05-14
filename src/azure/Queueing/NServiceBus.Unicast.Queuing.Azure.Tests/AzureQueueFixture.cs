using NUnit.Framework;

namespace NServiceBus.Unicast.Queuing.Azure.Tests
{
    using System;
    using MessageInterfaces.MessageMapper.Reflection;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Serializers.Json;

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
            client.ServerTimeout = TimeSpan.FromSeconds(10);
            nativeQueue = client.GetQueueReference(QueueName);

            nativeQueue.CreateIfNotExists();
            nativeQueue.Clear();


            sender = new AzureMessageQueueSender
                        {
                            Client = client,
                            MessageSerializer = new JsonMessageSerializer(new MessageMapper())
                        };

            sender.Init(QueueName, true);

            receiver = new AzureMessageQueueReceiver
            {
                Client = client,
                MessageSerializer = new JsonMessageSerializer(new MessageMapper()),
            };

            receiver.Init(QueueName, true);
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