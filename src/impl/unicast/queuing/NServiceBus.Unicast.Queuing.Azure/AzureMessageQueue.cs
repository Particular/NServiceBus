using System;
using System.Collections.Generic;
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
        private readonly Queue<CloudQueueMessage> messages = new Queue<CloudQueueMessage>();

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

        /// <summary>
        /// Controls the number of messages that will be read in bulk from the queue
        /// </summary>
        public int BatchSize { get; set; }

        public AzureMessageQueue(CloudQueueClient client)
        {
            this.client = client;
            MessageInvisibleTime = 30000;
            PeekInterval = 1000;
            MaximumWaitTimeWhenIdle = 60000;
            BatchSize = 10;
        }

        public void Init(string address, bool transactional)
        {
            Init(Address.Parse(address), transactional);
        }

        public void Init(Address address, bool transactional)
        {
            useTransactions = transactional;
            queue = client.GetQueueReference(address.Queue);
            queue.CreateIfNotExist();

			if (PurgeOnStartup)
				queue.Clear();
        }

        public void Send(TransportMessage message, string destination)
        {
            Send(message, Address.Parse(destination));
        }

        public void Send(TransportMessage message, Address address)
        {
            var sendQueue = client.GetQueueReference(address.Queue);

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
            if (messages.Count > 0) return true;

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
            if (messages.Count == 0)
            {
                var receivedMessages = queue.GetMessages(BatchSize, TimeSpan.FromMilliseconds(MessageInvisibleTime * BatchSize));

                foreach(var receivedMessage in receivedMessages)
                {
                    if (!useTransactions || Transaction.Current == null)
                        DeleteMessage(receivedMessage);
                    else
                        Transaction.Current.EnlistVolatile(new ReceiveResourceManager(queue, receivedMessage),
                                                            EnlistmentOptions.None);

                    messages.Enqueue(receivedMessage);
                }
            }

            return messages.Count != 0 ? messages.Dequeue() : null;
        }

        private void DeleteMessage(CloudQueueMessage message)
        {
            try
            {
                queue.DeleteMessage(message);
            }
            catch (StorageClientException ex)
            {
                if (ex.ErrorCode != StorageErrorCode.ResourceNotFound ) throw;
            }
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