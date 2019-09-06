namespace NServiceBus
{
    using ObjectBuilder;

    /// <summary>
    /// Provides factory methods for creating endpoints instances with an externally managed container.
    /// </summary>
    public static class EndpointWithExternallyManagedContainer
    {
        /// <summary>
        /// Creates a new startable endpoint based on the provided configuration that uses an externally managed container.
        /// </summary>
        /// <param name="configuration">The endpoint configuration.</param>
        /// <param name="configureComponents">The registration API adapter for the external container.</param>
        public static IStartableEndpointWithExternallyManagedContainer Create(EndpointConfiguration configuration, IConfigureComponents configureComponents)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(configureComponents), configureComponents);

            return EndpointCreator
                .CreateWithExternallyManagedContainer(configuration, configureComponents);
        }
    }
}