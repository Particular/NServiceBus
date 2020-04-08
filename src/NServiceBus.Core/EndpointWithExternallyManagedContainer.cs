namespace NServiceBus
{
    using System;
    using System.Runtime.ExceptionServices;
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
        public static (IStartableEndpointWithExternallyManagedContainer, ExceptionDispatchInfo) Create(EndpointConfiguration configuration, IConfigureComponents configureComponents)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(configureComponents), configureComponents);

            ExceptionDispatchInfo exceptionDispatchInfo = null;
            IStartableEndpointWithExternallyManagedContainer endpoint = null;
            try
            {
                endpoint = HostCreator
                    .CreateWithExternallyManagedContainer(configuration, configureComponents);
            }
            catch (Exception e)
            {
                exceptionDispatchInfo = ExceptionDispatchInfo.Capture(e);
            }

            return (endpoint, exceptionDispatchInfo);
        }
    }
}