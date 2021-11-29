namespace NServiceBus
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Transport;

    /// <summary>
    /// Extension methods to expose receive component related configuration to features.
    /// </summary>
    public static class ReceiveFeatureConfigurationContextExtensions
    {
        /// <summary>
        /// Returns the local queue address of this endpoint.
        /// </summary>
        public static QueueAddress LocalQueueAddress(this FeatureConfigurationContext config)
        {
            Guard.AgainstNull(nameof(config), config);

            if (config.Receiving.IsSendOnlyEndpoint)
            {
                throw new InvalidOperationException("LocalQueueAddress isn't available for send only endpoints.");
            }

            return config.Receiving.LocalQueueAddress;
        }

        /// <summary>
        /// Returns the instance specific queue address of this endpoint.
        /// </summary>
        public static QueueAddress InstanceSpecificQueueAddress(this FeatureConfigurationContext config)
        {
            Guard.AgainstNull(nameof(config), config);

            if (config.Receiving.IsSendOnlyEndpoint)
            {
                throw new InvalidOperationException("InstanceSpecificQueueAddress isn't available for send only endpoints.");
            }

            return config.Receiving.InstanceSpecificQueueAddress;
        }
    }
}