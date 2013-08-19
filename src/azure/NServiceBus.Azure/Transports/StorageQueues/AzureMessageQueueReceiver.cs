namespace NServiceBus.Unicast.Queuing.Azure
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Transactions;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Serialization;
    using Transports.StorageQueues;

    public class AzureMessageQueueReceiver
    {
        public const int DefaultMessageInvisibleTime = 30000;
        public const int DefaultPeekInterval = 50;
        public const int DefaultMaximumWaitTimeWhenIdle = 1000;
        public const int DefaultBatchSize = 10;
        public const bool DefaultPurgeOnStartup = false;
        public const string DefaultConnectionString = "UseDevelopmentStorage=true";
        public const bool DefaultQueuePerInstance = false;

        CloudQueue azureQueue;
        int timeToDelayNextPeek;
        bool useTransactions;
        ConcurrentQueue<CloudQueueMessage> batchQueue = new ConcurrentQueue<CloudQueueMessage>();

        /// <summary>
        /// Sets the amount of time, in milliseconds, to add to the time to wait before checking for a new message
        /// </summary>
        public int PeekInterval { get; set; }

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

        /// <summary>
        /// Gets or sets the message serializer
        /// </summary>
        public IMessageSerializer MessageSerializer { get; set; }

        public CloudQueueClient Client { get; set; }

        public AzureMessageQueueReceiver()
        {
            MessageInvisibleTime = DefaultMessageInvisibleTime;
            PeekInterval = DefaultPeekInterval;
            MaximumWaitTimeWhenIdle = DefaultMaximumWaitTimeWhenIdle;
            BatchSize = DefaultBatchSize;
        }

        public void Init(string address, bool transactional)
        {
            Init(Address.Parse(address), transactional);
        }

        public void Init(Address address, bool transactional)
        {
            useTransactions = transactional;

            var queueName = AzureMessageQueueUtils.GetQueueName(address);

            azureQueue = Client.GetQueueReference(queueName);
            azureQueue.CreateIfNotExists();

            if (PurgeOnStartup)
            {
                azureQueue.Clear();
            }
        }

        public TransportMessage Receive()
        {
            var rawMessage = GetMessage();

            if (rawMessage == null)
            {

                if (timeToDelayNextPeek < MaximumWaitTimeWhenIdle)
                {
                    timeToDelayNextPeek += PeekInterval;
                }

                Thread.Sleep(timeToDelayNextPeek);

                return null;
            }

            timeToDelayNextPeek = 0;
            try
            {
                return DeserializeMessage(rawMessage);
            }
            catch (Exception ex)
            {
                throw new EnvelopeDeserializationFailed(rawMessage, ex);
            }
            finally
            {
                if (!useTransactions || Transaction.Current == null)
                {
                    DeleteMessage(rawMessage);
                }
                else
                {
                    Transaction.Current.EnlistVolatile(new ReceiveResourceManager(azureQueue, rawMessage), EnlistmentOptions.None);
                }
            }
        }

        CloudQueueMessage GetMessage()
        {
            CloudQueueMessage cloudQueueMessage;
            if (batchQueue.TryDequeue(out cloudQueueMessage))
            {
                return cloudQueueMessage;
            }
            var callback = new AsyncCallback(ar =>
            {
                var receivedMessages = azureQueue.EndGetMessages(ar);
                foreach (var receivedMessage in receivedMessages)
                {
                    batchQueue.Enqueue(receivedMessage);
                }
            });
            azureQueue.BeginGetMessages(BatchSize, TimeSpan.FromMilliseconds(MessageInvisibleTime), null, null, callback, null);
            return null;
        }

        void DeleteMessage(CloudQueueMessage message)
        {
            try
            {
                azureQueue.DeleteMessage(message);
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode != 404)
                {
                    throw;
                }
            }
        }

        TransportMessage DeserializeMessage(CloudQueueMessage rawMessage)
        {
            using (var stream = new MemoryStream(rawMessage.AsBytes))
            {
                object[] deserializedObjects;
                try
                {
                    deserializedObjects = MessageSerializer.Deserialize(stream, new List<Type> { typeof(MessageWrapper) });
                }
                catch (Exception)
                {
                    throw new SerializationException("Failed to deserialize message with id: " + rawMessage.Id);
                }

                var m = deserializedObjects.FirstOrDefault() as MessageWrapper;

                if (m == null)
                {
                    throw new SerializationException("Failed to deserialize message with id: " + rawMessage.Id);
                }

                return new TransportMessage(m.Id, m.Headers)
                {
                    Body = m.Body,
                    CorrelationId = m.CorrelationId,
                    Recoverable = m.Recoverable,
                    ReplyToAddress = Address.Parse(m.ReplyToAddress),
                    TimeToBeReceived = m.TimeToBeReceived,
                    MessageIntent = m.MessageIntent
                };
            }
        }

    }
}
