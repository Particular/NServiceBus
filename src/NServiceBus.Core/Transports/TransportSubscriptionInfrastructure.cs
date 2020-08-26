namespace NServiceBus.Transport
{
    using System;

    /// <summary>
    /// Represents the result for configuring the transport for subscribing.
    /// </summary>
    public class TransportSubscriptionInfrastructure
    {
        /// <summary>
        /// Creates new result object.
        /// </summary>
        public TransportSubscriptionInfrastructure(Func<IManageSubscriptions> subscriptionManagerFactory)
        {
            Guard.AgainstNull(nameof(subscriptionManagerFactory), subscriptionManagerFactory);
            SubscriptionManagerFactory = subscriptionManagerFactory;
        }

        /// <summary>
        /// Factory to create the subscription manager.
        /// </summary>
        public Func<IManageSubscriptions> SubscriptionManagerFactory { get; }
    }
}