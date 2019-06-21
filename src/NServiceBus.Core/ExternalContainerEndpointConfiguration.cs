namespace NServiceBus
{
    using ObjectBuilder;

    /// <summary>
    /// Configuration used to create an endpoint instance.
    /// </summary>
    public class ExternalContainerEndpointConfiguration : EndpointConfigurationBase
    {
        IConfigureComponents configurator;

        /// <summary>
        /// Initializes the endpoint configuration builder.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint being configured.</param>
        /// <param name="configurator">An externally owned component configurator.</param>
        public ExternalContainerEndpointConfiguration(string endpointName, IConfigureComponents configurator)
            : base(endpointName)
        {
            this.configurator = configurator;
        }

        internal IConfiguredEndpoint Configure()
        {
            var configurable = CreateConfigurable(configurator);
            var configured = configurable.Configure();
            return configured;
        }
    }
}