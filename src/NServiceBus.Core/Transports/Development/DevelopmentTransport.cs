namespace NServiceBus
{
    using Settings;
    using Transport;

    /// <summary>
    /// A transport optimized for development use. DO NOT use in production.
    /// </summary>
    public class DevelopmentTransport : TransportDefinition
    {
        /// <summary>
        /// Used by implementations to control if a connection string is necessary.
        /// </summary>
        public override bool RequiresConnectionString => false;

        /// <summary>
        /// Gets an example connection string to use when reporting lack of configured connection string to the user.
        /// </summary>
        public override string ExampleConnectionStringForErrorMessage { get; } = "";

        /// <summary>
        /// Initializes all the factories and supported features for the transport. This method is called right before all features
        /// are activated and the settings will be locked down. This means you can use the SettingsHolder both for providing
        /// default capabilities as well as for initializing the transport's configuration based on those settings (the user cannot
        /// provide information anymore at this stage).
        /// </summary>
        /// <param name="settings">An instance of the current settings.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The supported factories.</returns>
        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            return new DevelopmentTransportInfrastructure(settings);
        }
    }
}