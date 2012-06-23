using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Transactions;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using NServiceBus.Serialization;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast.Queuing.Azure
{
    public class AzureMessageQueueReceiver : IReceiveMessages
    {
        public const int DefaultMessageInvisibleTime = 30000;
        public const int DefaultPeekInterval = 50;
        public const int DefaultMaximumWaitTimeWhenIdle = 1000;
        public const int DefaultBatchSize = 10;
        public const bool DefaultPurgeOnStartup = false;
        public const string DefaultConnectionString = "UseDevelopmentStorage=true";
        public const bool DefaultQueuePerInstance = false;

        private CloudQueue queue;
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
            queue = Client.GetQueueReference(SanitizeQueueName(address.Queue));
            queue.CreateIfNotExist();

			if (PurgeOnStartup)
				queue.Clear();
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

        private TransportMessage DeserializeMessage(CloudQueueMessage rawMessage)
        {
            using (var stream = new MemoryStream(rawMessage.AsBytes))
            {
                var m = MessageSerializer.Deserialize(stream).FirstOrDefault() as MessageWrapper;
                
                if (m == null)
                    throw new SerializationException("Failed to deserialize message with id: " + rawMessage.Id);

                var message = new TransportMessage
                {
                    Id = m.Id,
                    Body = m.Body,
                    CorrelationId = m.CorrelationId,
                    Recoverable = m.Recoverable,
                    ReplyToAddress = Address.Parse(m.ReplyToAddress),
                    TimeToBeReceived = m.TimeToBeReceived,
                    Headers = m.Headers,
                    MessageIntent = m.MessageIntent,
                    TimeSent = m.TimeSent,
                    IdForCorrelation = m.IdForCorrelation
                };

                message.Id = GetRealId(message.Headers) ?? message.Id;

                message.IdForCorrelation = GetIdForCorrelation(message.Headers) ?? message.Id;

                return message;
            }
        }

        public void CreateQueue(string queueName)
        {
            Client.GetQueueReference(SanitizeQueueName(queueName)).CreateIfNotExist();
        }

        private string SanitizeQueueName(string queueName)
        {
            // The auto queue name generation uses namespaces which includes dots, 
            // yet dots are not supported in azure storage names
            // that's why we replace them here.

            return queueName.Replace('.', '-');
        }

        private static string GetRealId(IDictionary<string, string> headers)
        {
            if (headers.ContainsKey(TransportHeaderKeys.OriginalId))
                return headers[TransportHeaderKeys.OriginalId];

            return null;
        }

        private static string GetIdForCorrelation(IDictionary<string, string> headers)
        {
            if (headers.ContainsKey(Idforcorrelation))
                return headers[Idforcorrelation];

            return null;
        }

        private bool useTransactions;
        private const string Idforcorrelation = "CorrId";

        
    }

    public class AzureMessageQueueSender : ISendMessages
    {
        private readonly Dictionary<string, CloudQueueClient> destinationQueueClients = new Dictionary<string, CloudQueueClient>();
        private static readonly object SenderLock = new Object();
        /// <summary>
        /// Gets or sets the message serializer
        /// </summary>
        public IMessageSerializer MessageSerializer { get; set; }

        public CloudQueueClient Client { get; set; }

        public void Init(string address, bool transactional)
        {
            Init(Address.Parse(address), transactional);
        }

        public void Init(Address address, bool transactional)
        {
            
        }

        public void Send(TransportMessage message, string destination)
        {
            Send(message, Address.Parse(destination));
        }

        public void Send(TransportMessage message, Address address)
        {
            var sendClient = GetClientForConnectionString(address.Machine) ?? Client;

            var sendQueue = sendClient.GetQueueReference(SanitizeQueueName(address.Queue));

            if (!sendQueue.Exists())
                throw new QueueNotFoundException();

            if (string.IsNullOrEmpty(message.Id)) message.Id = Guid.NewGuid().ToString();

            var rawMessage = SerializeMessage(message);

            if (Transaction.Current == null)
            {
                sendQueue.AddMessage(rawMessage);
            }
            else
                Transaction.Current.EnlistVolatile(new SendResourceManager(sendQueue, rawMessage), EnlistmentOptions.None);
        }

        private CloudQueueClient GetClientForConnectionString(string connectionString)
        {
            CloudQueueClient sendClient;

            if (!destinationQueueClients.TryGetValue(connectionString, out sendClient))
            {
                lock (SenderLock)
                {
                    if (!destinationQueueClients.TryGetValue(connectionString, out sendClient))
                    {
                        CloudStorageAccount account;

                        if (CloudStorageAccount.TryParse(connectionString, out account))
                        {
                            sendClient = account.CreateCloudQueueClient();
                        }

                        // sendClient could be null, this is intentional 
                        // so that it remembers a connectionstring was invald 
                        // and doesn't try to parse it again.

                        destinationQueueClients.Add(connectionString, sendClient);
                    }
                }
            }

            return sendClient;
        }

        private CloudQueueMessage SerializeMessage(TransportMessage message)
        {
            using (var stream = new MemoryStream())
            {

                if (message.Headers == null)
                    message.Headers = new Dictionary<string, string>();

                if (!message.Headers.ContainsKey(Idforcorrelation))
                    message.Headers.Add(Idforcorrelation, null);

                if (String.IsNullOrEmpty(message.Headers[Idforcorrelation]))
                    message.Headers[Idforcorrelation] = message.IdForCorrelation;

                var toSend = new MessageWrapper
                {
                    Id = message.Id,
                    Body = message.Body,
                    CorrelationId = message.CorrelationId,
                    Recoverable = message.Recoverable,
                    ReplyToAddress = message.ReplyToAddress.ToString(),
                    TimeToBeReceived = message.TimeToBeReceived,
                    Headers = message.Headers,
                    MessageIntent = message.MessageIntent,
                    TimeSent = message.TimeSent,
                    IdForCorrelation = message.IdForCorrelation
                };


                MessageSerializer.Serialize(new IMessage[] { toSend }, stream);
                return new CloudQueueMessage(stream.ToArray());
            }
        }

        public void CreateQueue(string queueName)
        {
            Client.GetQueueReference(SanitizeQueueName(queueName)).CreateIfNotExist();
        }

        private string SanitizeQueueName(string queueName)
        {
            // The auto queue name generation uses namespaces which includes dots, 
            // yet dots are not supported in azure storage names
            // that's why we replace them here.

            return queueName.Replace('.', '-');
        }

        private const string Idforcorrelation = "CorrId";
    }


    [Serializable]
    internal class MessageWrapper : IMessage
    {
        public string IdForCorrelation { get; set; }
        public DateTime TimeSent { get; set; }
        public string Id { get; set; }
        public MessageIntentEnum MessageIntent { get; set; }
        public string ReplyToAddress { get; set; }
        public TimeSpan TimeToBeReceived { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public byte[] Body { get; set; }
        public string CorrelationId { get; set; }
        public bool Recoverable { get; set; }
    }
}