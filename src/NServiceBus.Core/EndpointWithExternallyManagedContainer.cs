namespace NServiceBus
{
    using ObjectBuilder;

    /// <summary>
    /// Factory methods for configuring endpoints with an externally managed container.
    /// </summary>
    public static class EndpointWithExternallyManagedContainer
    {
        /// <summary>
        /// Configures an endpoint to use an externally managed container.
        /// </summary>
        /// <param name="configuration">The endpoint configuration.</param>
        /// <param name="configureComponents">The registration API adapter for the external container.</param>
        /// <returns>The configured endpoint.</returns>
        public static IConfiguredEndpointWithExternallyManagedContainer Configure(EndpointConfiguration configuration, IConfigureComponents configureComponents)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(configureComponents), configureComponents);

            return ConfiguredEndpoint
                .ConfigureWithExternallyManagedContainer(configuration, configureComponents);
        }
    }
}