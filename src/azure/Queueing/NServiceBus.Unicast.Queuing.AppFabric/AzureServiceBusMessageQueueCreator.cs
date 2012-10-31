using System;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    public class AzureServiceBusMessageQueueCreator : ICreateQueues
    {
        public string ConnectionString { get; set; }
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

        public void CreateQueueIfNecessary(Address address, string account)
        {
            try
            {
                var queueName = address.Queue;
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

                if (!NamespaceClient.QueueExists(queueName))
                    NamespaceClient.CreateQueue(description);
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // the queue already exists, which is ok
            }
        }
    }
}