namespace NServiceBus
{
    /// <summary>
    /// Extension methods to configure the local input address of the endpoint.
    /// </summary>
    public static class LocalAddressConfigurationExtensions
    {
        /// <summary>
        /// Sets the address of this endpoint.
        /// </summary>
        /// <param name="endpointConfiguration">The endpoint configuration to extend.</param>
        /// <param name="localAddress">The queue name.</param>
        public static void OverrideLocalAddress(this EndpointConfiguration endpointConfiguration, string localAddress)
        {
            Guard.AgainstNullAndEmpty(nameof(localAddress), localAddress);

            endpointConfiguration.Settings.Set("LocalAddressOverride", localAddress);
        }
    }
}