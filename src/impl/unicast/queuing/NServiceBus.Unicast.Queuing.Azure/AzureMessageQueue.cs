using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Transactions;
using Microsoft.WindowsAzure.StorageClient;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast.Queuing.Azure
{
    public class AzureMessageQueue : IReceiveMessages,ISendMessages
    {
        private CloudQueue queue;
        private readonly CloudQueueClient client;
        private int timeToDelayNextPeek;

        /// <summary>
        /// Sets the amount of time, in milliseconds, to add to the time to wait before checking for a new message
        /// </summary>
        public int PeekInterval{ get; set; }

        /// <summary>
        /// Sets the maximum amount of time, in milliseconds, that the queue will wait before checking for a new message
        /// </summary>
        public int MaximumWaitTimeWhenIdle { get; set; }
   
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
            MessageInvisibleTime = 30000;
            PeekInterval = 1000;
            MaximumWaitTimeWhenIdle = 60000;
        }

        public void Init(string address, bool transactional)
        {
            useTransactions = transactional;
            queue = client.GetQueueReference(address);
            queue.CreateIfNotExist();

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
            var hasMessage = queue.PeekMessage() != null;

            DelayNextPeekWhenThereIsNoMessage(hasMessage);
            
            return hasMessage;
        }

        private void DelayNextPeekWhenThereIsNoMessage(bool hasMessage)
        {
            if (hasMessage)
            {
                timeToDelayNextPeek = 0;
            }
            else
            {
                if (timeToDelayNextPeek < MaximumWaitTimeWhenIdle) timeToDelayNextPeek += PeekInterval;

                Thread.Sleep(timeToDelayNextPeek);
            }
        }

        public TransportMessage Receive()
        {
            var rawMessage = GetMessage();

            if (rawMessage == null)
                return null;

            return DeserializeMessage(rawMessage);
        }

        private CloudQueueMessage GetMessage()
        {
            var receivedMessage = queue.GetMessage(TimeSpan.FromMilliseconds(MessageInvisibleTime));

            if (receivedMessage != null)
            {
                if (!useTransactions || Transaction.Current == null)
                    queue.DeleteMessage(receivedMessage);
                else
                    Transaction.Current.EnlistVolatile(new ReceiveResourceManager(queue, receivedMessage),EnlistmentOptions.None);
            }

            return receivedMessage;
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

        private bool useTransactions;
    }
}