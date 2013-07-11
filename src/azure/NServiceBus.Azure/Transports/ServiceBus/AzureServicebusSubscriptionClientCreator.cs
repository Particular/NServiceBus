namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using System;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// 
    /// </summary>
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
            var subscriptionname = AzureServiceBusSubscriptionNameConvention.Create(eventType);
            return Create(eventType, topicPath, subscriptionname);
        }

        public SubscriptionClient Create(Type eventType, string topicPath, string subscriptionname)
        {
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

                        if (eventType != null)
                        {
                            var filter =
                                string.Format(
                                    "[{0}] LIKE '{1}%' OR [{0}] LIKE '%{1}%' OR [{0}] LIKE '%{1}' OR [{0}] = '{1}'",
                                    Headers.EnclosedMessageTypes, eventType.AssemblyQualifiedName);
                            var typefilter = new SqlFilter(filter);

                            NamespaceClient.CreateSubscription(description, typefilter);
                        }
                        else
                        {
                            NamespaceClient.CreateSubscription(description);
                        }
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