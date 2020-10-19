namespace NServiceBus.TransportTests
{
    using Transport;

    /// <summary>
    /// Transport configuration result returned by <see cref="IConfigureTransportInfrastructure"/>.
    /// </summary>
    public class TransportConfigurationResult
    {
        /// <summary>
        /// </summary>
        public TransportDefinition TransportDefinition { get; set; }

        /// <summary>
        /// Flag representing if input queue should be purged before running any test.
        /// </summary>
        public bool PurgeInputQueueOnStartup { get; set; }

        /// <summary>
        /// The runtime settings to use.
        /// </summary>
        public PushRuntimeSettings PushRuntimeSettings { get; set; } = PushRuntimeSettings.Default;


        /// <summary>
        /// Creates <see cref="TransportConfigurationResult"/>.
        /// </summary>
        public TransportConfigurationResult()
        {
            PurgeInputQueueOnStartup = true;
        }
    }
}