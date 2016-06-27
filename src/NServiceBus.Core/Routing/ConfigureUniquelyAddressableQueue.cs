namespace NServiceBus
{
    /// <summary>
    /// Contains an <see cref="EndpointConfiguration" /> extension method to add a uniquely addressable, endpoint-specifc queue.
    /// </summary>
    public static class ConfigureUniquelyAddressableQueue
    {
        /// <summary>
        ///Adds a uniquely addressable, endpoint-specifc queue in addition to the main, shared queue.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="discriminator">The value to append to the endpoint name to create a uniquely named queue.</param>
        public static void AddUniquelyAddressableQueue(this EndpointConfiguration config, string discriminator)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNullAndEmpty(nameof(discriminator), discriminator);

            config.Settings.Set("EndpointInstanceDiscriminator", discriminator);
        }
    }
}
