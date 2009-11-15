using System;
using Microsoft.Samples.ServiceHosting.StorageClient;
using NServiceBus.Unicast.Queuing;
using NUnit.Framework;

namespace NServiceBus.Unicast.Queueing.Azure.Tests
{
    public abstract class AzureQueueFixture
    {
        protected AzureMessageQueue queue;
        protected QueueStorage storage;
        protected MessageQueue nativeQueue;


        protected virtual string QueueName
        {
            get
            {
                return "testqueue";
            }
        }

        protected virtual bool PurgeOnStartup { get{ return false;} }

        protected virtual int SecondsToWaitForMessage { get { return 1; } }

        [SetUp]
        public void Setup()
        {
            storage = QueueStorage.Create(new Uri("http://127.0.0.1:10001"), true, "devstoreaccount1",
                                          "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==");
            
            nativeQueue = storage.GetQueue(QueueName);

            if (nativeQueue.DoesQueueExist())
                nativeQueue.Clear();

            nativeQueue.CreateQueue();

            queue = new AzureMessageQueue(storage);

            queue.Init(QueueName, PurgeOnStartup, SecondsToWaitForMessage);
        }

        protected void AddTestMessage()
        {
            AddTestMessage(new QueuedMessage());
        }

        protected void AddTestMessage(QueuedMessage messageToAdd)
        {
            queue.Send(messageToAdd,QueueName,false);
        }

    }
}