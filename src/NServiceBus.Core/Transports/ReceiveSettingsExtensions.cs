namespace NServiceBus
{
    /// <summary>
    /// Configuration extensions for receive settings.
    /// </summary>
    public static class ReceiveSettingsExtensions
    {
        /// <summary>
        ///Makes the endpoint instance uniquely addressable when running multiple instances by adding an instance-specific queue.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="discriminator">The value to append to the endpoint name to create an instance-specific queue.</param>
        public static void MakeInstanceUniquelyAddressable(this EndpointConfiguration config, string discriminator)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNullAndEmpty(nameof(discriminator), discriminator);

            config.Settings.Set("EndpointInstanceDiscriminator", discriminator);
        }

        /// <summary>
        /// Overrides the base name of the input queue. The actual input queue name consists of this base name, instance ID and subqueue qualifier.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="baseInputQueueName">The base name of the input queue.</param>
        public static void OverrideLocalAddress(this EndpointConfiguration config, string baseInputQueueName)
        {
            Guard.AgainstNullAndEmpty(nameof(baseInputQueueName), baseInputQueueName);
            config.Settings.SetDefault("BaseInputQueueName", baseInputQueueName);
        }
    }
}
