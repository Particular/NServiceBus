using System;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    public class AzureServicebusQueueClientCreator : ICreateQueueClients
    {
        public MessagingFactory Factory { get; set; }
        public NamespaceManager NamespaceClient { get; set; }

        public TimeSpan LockDuration { get; set; }
        public long MaxSizeInMegabytes { get; set; }
        public bool RequiresDuplicateDetection { get; set; }
        public bool RequiresSession { get; set; }
        public TimeSpan DefaultMessageTimeToLive { get; set; }
        public bool EnableDeadLetteringOnMessageExpiration { get; set; }
        public TimeSpan DuplicateDetectionHistoryTimeWindow { get; set; }
        public int MaxDeliveryCount { get; set; }
        public bool EnableBatchedOperations { get; set; }

        public QueueClient Create(Address address)
        {
            var queueName = address.Queue;
            try
            {
                if (!NamespaceClient.QueueExists(queueName))
                {
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
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // the queue already exists or another node beat us to it, which is ok
            }

            var client = Factory.CreateQueueClient(queueName, ReceiveMode.PeekLock);
            client.PrefetchCount = 100; // todo make configurable
            return client;
        }
    }
}