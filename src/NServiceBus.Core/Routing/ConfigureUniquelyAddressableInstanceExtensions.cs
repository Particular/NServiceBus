namespace NServiceBus
{
    /// <summary>
    /// Contains an <see cref="EndpointConfiguration" /> extension method to specify an instance discriminator for an instance-specific queue.
    /// </summary>
    public static class ConfigureUniquelyAddressableInstanceExtensions
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
    }
}
