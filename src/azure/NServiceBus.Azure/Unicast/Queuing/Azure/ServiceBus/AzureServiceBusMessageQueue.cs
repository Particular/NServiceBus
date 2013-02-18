namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading;
    using System.Transactions;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using Transport;

    /// <summary>
    /// 
    /// </summary>
    public class AzureServiceBusMessageQueueReceiver : IReceiveMessages
    {
        public const string DefaultIssuerName = "owner";
        public const int DefaultLockDuration = 30000;
        public const long DefaultMaxSizeInMegabytes = 1024;
        public const bool DefaultRequiresDuplicateDetection = false;
        public const bool DefaultRequiresSession = false;
        public const long DefaultDefaultMessageTimeToLive = 92233720368547;
        public const bool DefaultEnableDeadLetteringOnMessageExpiration = false;
        public const int DefaultDuplicateDetectionHistoryTimeWindow = 600000;
        public const int DefaultMaxDeliveryCount = 6;
        public const bool DefaultEnableBatchedOperations = false;
        public const bool DefaultQueuePerInstance = false;
        public const int DefaultBackoffTimeInSeconds = 10;
        public const int DefaultServerWaitTime = 300;
        public const string DefaultConnectivityMode = "Tcp";
        public const string DefaultConnectionString = "";

        private bool useTransactions;
        private QueueClient queueClient;
        private string queueName;

        public TimeSpan LockDuration { get; set; }
        public long MaxSizeInMegabytes { get; set; }
        public bool RequiresDuplicateDetection { get; set; }
        public bool RequiresSession { get; set; }
        public TimeSpan DefaultMessageTimeToLive { get; set; }
        public bool EnableDeadLetteringOnMessageExpiration { get; set; }
        public TimeSpan DuplicateDetectionHistoryTimeWindow { get; set; }
        public int MaxDeliveryCount { get; set; }
        public bool EnableBatchedOperations { get; set; }
        public int ServerWaitTime { get; set; }

        public MessagingFactory Factory { get; set; }
        public NamespaceManager NamespaceClient { get; set; }

        public void Init(string address, bool transactional)
        {
            Init(Address.Parse(address), transactional);
        }

        public void Init(Address address, bool transactional)
        {
            try
            {
                queueName = address.Queue;
                var description = new QueueDescription(queueName)
                                      {
                                          LockDuration = LockDuration,
                                          MaxSizeInMegabytes = MaxSizeInMegabytes,
                                          RequiresDuplicateDetection = RequiresDuplicateDetection,
                                          RequiresSession = RequiresSession,
                                          DefaultMessageTimeToLive = DefaultMessageTimeToLive,
                                          EnableDeadLetteringOnMessageExpiration = EnableDeadLetteringOnMessageExpiration,
                                          DuplicateDetectionHistoryTimeWindow = DuplicateDetectionHistoryTimeWindow,
                                          MaxDeliveryCount = MaxDeliveryCount,
                                          EnableBatchedOperations = EnableBatchedOperations
                                      };

                NamespaceClient.CreateQueue(description);
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // the queue already exists, which is ok
            }

            queueClient = Factory.CreateQueueClient(queueName, ReceiveMode.PeekLock);

            useTransactions = transactional;
        }

        public TransportMessage Receive()
        {
            try
            {
                var message = queueClient.Receive(TimeSpan.FromSeconds(ServerWaitTime));

                if (message != null)
                {
                    var rawMessage = message.GetBody<byte[]>();

                    TransportMessage t;

                    if (message.Properties.Count == 0)
                    {
                        t = DeserializeMessage(rawMessage);
                    }
                    else
                    {
                        t = new TransportMessage();
                        if (!string.IsNullOrWhiteSpace(message.CorrelationId)) t.CorrelationId = message.CorrelationId;
                        t.TimeToBeReceived = message.TimeToLive;

                        foreach (var header in message.Properties)
                        {
                            t.Headers[header.Key] = header.Value.ToString();
                        }

                        t.MessageIntent = (MessageIntentEnum) Enum.Parse(typeof (MessageIntentEnum), message.Properties["MessageIntent"].ToString());
                        t.Id = message.MessageId;
                        t.ReplyToAddress = Address.Parse(message.ReplyTo); // Will this work?

                        t.Body = rawMessage;
                    }

                    if (t.Id == null) t.Id = Guid.NewGuid().ToString();

                    if (!useTransactions || Transaction.Current == null)
                    {
                        using (message)
                        {
                            message.SafeComplete();
                        }
                    }
                    else if (Transaction.Current.TransactionInformation.Status == TransactionStatus.Active)
                    {
                        Transaction.Current.EnlistVolatile(new ReceiveResourceManager(message), EnlistmentOptions.None);
                    }
                    else
                    {
                        return null;
                    }

                    return t;
                }
            }
            // back off when we're being throttled
            catch (ServerBusyException)
            {
                Thread.Sleep(TimeSpan.FromSeconds(DefaultBackoffTimeInSeconds));
            }
            catch (TimeoutException)
            {
                return null;
            }

            return null;
        }

        private static TransportMessage DeserializeMessage(byte[] rawMessage)
        {
            var formatter = new BinaryFormatter();

            using (var stream = new MemoryStream(rawMessage))
            {
                var message = formatter.Deserialize(stream) as TransportMessage;

                if (message == null)
                    throw new SerializationException("Failed to deserialize message");

                return message;
            }
        }
    }
}
