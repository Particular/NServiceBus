namespace NServiceBus
{
    using NServiceBus.Features;
    using NServiceBus.Transport;

    /// <summary>
    /// Extension methods to configure hostid.
    /// </summary>
    public static class ReceiveFeatureConfigurationContextExtensions
    {
        /// <summary>
        /// Returns the local queue address of this endpoint.
        /// </summary>
        public static QueueAddress LocalQueueAddress(this FeatureConfigurationContext config)
        {
            Guard.AgainstNull(nameof(config), config);

            return config.Receiving.LocalQueueAddress;
        }

        /// <summary>
        /// Returns the instance specific queue address of this endpoint.
        /// </summary>
        public static QueueAddress InstanceSpecificQueueAddress(this FeatureConfigurationContext config)
        {
            Guard.AgainstNull(nameof(config), config);

            return config.Receiving.InstanceSpecificQueueAddress;
        }
    }
}