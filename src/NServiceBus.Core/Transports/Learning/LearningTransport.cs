namespace NServiceBus
{
    using Settings;
    using Transport;

    /// <summary>
    /// A transport optimized for development and learning use. DO NOT use in production.
    /// </summary>
    public class LearningTransport : TransportDefinition
    {

        /// <summary>
        /// 
        /// </summary>
        public bool RestrictPayloadSize { get; set; } = true;

        /// <summary>
        /// 
        /// </summary>
        public string StorageDirectory { get; set; }

        /// <summary>
        /// Used by implementations to control if a connection string is necessary.
        /// </summary>
        public override bool RequiresConnectionString => false;

        /// <summary>
        /// Gets an example connection string to use when reporting the lack of a configured connection string to the user.
        /// </summary>
        public override string ExampleConnectionStringForErrorMessage { get; } = "";

        /// <summary>
        /// Initializes all the factories and supported features for the transport. This method is called right before all features
        /// are activated and the settings will be locked down. This means you can use the SettingsHolder both for providing
        /// default capabilities as well as for initializing the transport's configuration based on those settings (the user cannot
        /// provide information anymore at this stage).
        /// </summary>
        public override TransportInfrastructure Initialize(TransportSettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);
            return new LearningTransportInfrastructure(settings, this);
        }
    }
}