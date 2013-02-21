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

            return Factory.CreateQueueClient(queueName, ReceiveMode.PeekLock);
        }
    }

    public class AzureServicebusSubscriptionClientCreator : ICreateSubscriptionClients
    {
        public MessagingFactory Factory { get; set; }
        public NamespaceManager NamespaceClient { get; set; }

        public TimeSpan LockDuration { get; set; }
        public bool RequiresSession { get; set; }
        public TimeSpan DefaultMessageTimeToLive { get; set; }
        public bool EnableDeadLetteringOnMessageExpiration { get; set; }
        public int MaxDeliveryCount { get; set; }
        public bool EnableBatchedOperations { get; set; }
        public bool EnableDeadLetteringOnFilterEvaluationExceptions { get; set; }

        public SubscriptionClient Create(Address address, Type eventType)
        {
            var topicPath = address.Queue;
            var subscriptionname = Configure.EndpointName + "." + eventType.Name;
            if (NamespaceClient.TopicExists(topicPath))
            {
                try
                {
                    if (!NamespaceClient.SubscriptionExists(topicPath, subscriptionname))
                    {
                        var description = new SubscriptionDescription(topicPath, subscriptionname)
                            {
                                LockDuration = LockDuration,
                                RequiresSession = RequiresSession,
                                DefaultMessageTimeToLive = DefaultMessageTimeToLive,
                                EnableDeadLetteringOnMessageExpiration = EnableDeadLetteringOnMessageExpiration,
                                MaxDeliveryCount = MaxDeliveryCount,
                                EnableBatchedOperations = EnableBatchedOperations,
                                EnableDeadLetteringOnFilterEvaluationExceptions =
                                    EnableDeadLetteringOnFilterEvaluationExceptions
                            };

                        var typefilter =
                            new SqlFilter(Headers.EnclosedMessageTypes + " LIKE ‘" + eventType.AssemblyQualifiedName + "’");

                        NamespaceClient.CreateSubscription(description, typefilter);
                    }
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                    // the queue already exists or another node beat us to it, which is ok
                }

                return Factory.CreateSubscriptionClient(topicPath, subscriptionname, ReceiveMode.PeekLock);
            }
            return null;
        }
    }
}