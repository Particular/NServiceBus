namespace NServiceBus
{
    using System;
    using Features;
    using ObjectBuilder;
    using Transport;

    /// <summary>
    /// Provides the required components to use publish subscribe messaging patterns.
    /// </summary>
    public interface IPublishSubscribeProvider
    {
        /// <summary>
        /// Returns a <see cref="IPublishRouter"/> factory.
        /// </summary>
        Func<IBuilder, IPublishRouter> GetRouter(FeatureConfigurationContext context);

        /// <summary>
        /// Returns a <see cref="IManageSubscriptions"/> factory.
        /// </summary>
        Func<IBuilder, IManageSubscriptions> GetSubscriptionManager(FeatureConfigurationContext context);
    }
}