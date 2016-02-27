namespace NServiceBus.Transports.Msmq
{
    using System;
    using NServiceBus.Transports;

    /// <summary>
    /// Subscription store infrastructure for MSMQ transport.
    /// </summary>
    public class SubscriptionStoreInfrastructure
    {
        /// <summary>
        /// Creates new instance of subscription store infrastructure.
        /// </summary>
        /// <param name="subscriptionReaderFactory">Store reader factory.</param>
        /// <param name="subscriptionManagerFactory">Store manager factory.</param>
        public SubscriptionStoreInfrastructure(Func<IQuerySubscriptions> subscriptionReaderFactory, Func<IManageSubscriptions> subscriptionManagerFactory)
        {
            SubscriptionReaderFactory = subscriptionReaderFactory;
            SubscriptionManagerFactory = subscriptionManagerFactory;
        }

        internal Func<IQuerySubscriptions> SubscriptionReaderFactory { get; } 
        internal Func<IManageSubscriptions> SubscriptionManagerFactory { get; }
    }
}