using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Transactions;
using Microsoft.WindowsAzure.StorageClient;
using NServiceBus.Unicast.Queuing;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast.Queuing.Azure
{
    public class AzureMessageQueue : IMessageQueue
    {
        private CloudQueue queue;
        private readonly CloudQueueClient client;

        public int SecondsToWaitForMessage{ get; set;}
   
        /// <summary>
        /// Sets whether or not the transport should purge the input
        /// queue when it is started.
        /// </summary>
        public bool PurgeOnStartup { get; set; }

        /// <summary>
        /// Controls how long messages should be invisible to other callers when receiving messages from the queue
        /// </summary>
        public int MessageInvisibleTime { get; set; }

        public AzureMessageQueue(CloudQueueClient client)
        {
            this.client = client;
            MessageInvisibleTime = 30;
            SecondsToWaitForMessage = 1;
        }

        public void Init(string inputqueue)
        {
            queue = client.GetQueueReference(inputqueue);

            if (PurgeOnStartup)
                queue.Clear();
        }

        public void Send(TransportMessage message, string destination)
        {
            var sendQueue = client.GetQueueReference(destination);

            if (!sendQueue.Exists())
                throw new QueueNotFoundException();


            message.Id = Guid.NewGuid().ToString();

            var rawMessage = SerializeMessage(message);

            if (Transaction.Current == null)
                sendQueue.AddMessage(rawMessage);
            else
                Transaction.Current.EnlistVolatile(new SendResourceManager(sendQueue, rawMessage), EnlistmentOptions.None);

            
        }

        public bool HasMessage()
        {
            return queue.PeekMessage() != null;
        }

        public TransportMessage Receive(bool transactional)
        {
            var rawMessage = GetMessage(transactional);

            if (rawMessage == null)
                return null;

            return DeserializeMessage(rawMessage);
        }

        private CloudQueueMessage GetMessage(bool transactional)
        {
            var receivedMessage = PollForMessage();

            if (receivedMessage != null)
            {
                if (!transactional || Transaction.Current == null)
                    queue.DeleteMessage(receivedMessage);
                else
                    Transaction.Current.EnlistVolatile(new ReceiveResourceManager(queue, receivedMessage),EnlistmentOptions.None);
            }

            return receivedMessage;
        }

        private CloudQueueMessage PollForMessage()
        {
            CloudQueueMessage message;
            var maxTime = DateTime.Now.AddSeconds(SecondsToWaitForMessage);


            while ((message = queue.GetMessage(TimeSpan.FromSeconds(MessageInvisibleTime))) == null && DateTime.Now < maxTime)
            {
                Thread.Sleep(500);
            }

            return message;
        }

        private static CloudQueueMessage SerializeMessage(TransportMessage originalMessage)
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, originalMessage);
                return new CloudQueueMessage(stream.ToArray());
            }
        }

        private static TransportMessage DeserializeMessage(CloudQueueMessage rawMessage)
        {
            var formatter = new BinaryFormatter();

            using (var stream = new MemoryStream(rawMessage.AsBytes))
            {
                var message = formatter.Deserialize(stream) as TransportMessage;

                if (message == null)
                    throw new SerializationException("Failed to deserialize message with id: " + rawMessage.Id);

                return message;
            }
        }

        public void CreateQueue(string queueName)
        {
            client.GetQueueReference(queueName).CreateIfNotExist();
        }
    }
}