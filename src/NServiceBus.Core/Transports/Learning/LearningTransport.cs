using System.Threading.Tasks;

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
        /// Initializes all the factories and supported features for the transport. This method is called right before all features
        /// are activated and the settings will be locked down. This means you can use the SettingsHolder both for providing
        /// default capabilities as well as for initializing the transport's configuration based on those settings (the user cannot
        /// provide information anymore at this stage).
        /// </summary>
        public override Task<TransportInfrastructure> Initialize(TransportSettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);
            var learningTransportInfrastructure = new LearningTransportInfrastructure(settings, this);
            // here async initialzation of the sender could happen
            learningTransportInfrastructure.ConfigureSendInfrastructure();
            return Task.FromResult<TransportInfrastructure>(learningTransportInfrastructure);
        }
    }
}