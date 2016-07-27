namespace NServiceBus.TransportTests
{
    using Transport;

    /// <summary>
    /// Transport configuration result returned by <see cref="IConfigureTransportInfrastructure"/>.
    /// </summary>
    public class TransportConfigurationResult
    {

        /// <summary>
        /// Transport infrastructure.
        /// </summary>
        public TransportInfrastructure TransportInfrastructure { get; set; }

        /// <summary>
        /// Flag representing if input queue should be purged before running any test.
        /// </summary>
        public bool PurgeInputQueueOnStartup { get; set; }


        /// <summary>
        /// Creates <see cref="TransportConfigurationResult"/>.
        /// </summary>
        public TransportConfigurationResult()
        {
            PurgeInputQueueOnStartup = true;
        }
    }
}