using System.Threading.Tasks;

namespace NServiceBus
{
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
        public override Task<TransportInfrastructure> Initialize(Transport.Settings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);
            var learningTransportInfrastructure = new LearningTransportInfrastructure(settings, this);
            // here async initialzation of the sender could happen
            learningTransportInfrastructure.ConfigureSendInfrastructure();
            return Task.FromResult<TransportInfrastructure>(learningTransportInfrastructure);
        }

        /// <summary>
        /// </summary>
        public override string ToTransportAddress(EndpointAddress endpointAddress)
        {
            var address = endpointAddress.Endpoint;
            PathChecker.ThrowForBadPath(address, "endpoint name");

            var discriminator = endpointAddress.Discriminator;

            if (!string.IsNullOrEmpty(discriminator))
            {
                PathChecker.ThrowForBadPath(discriminator, "endpoint discriminator");

                address += "-" + discriminator;
            }

            var qualifier = endpointAddress.Qualifier;

            if (!string.IsNullOrEmpty(qualifier))
            {
                PathChecker.ThrowForBadPath(qualifier, "address qualifier");

                address += "-" + qualifier;
            }

            return address;
        }

        /// <summary>
        /// </summary>
        public override TransportTransactionMode MaxSupportedTransactionMode => TransportTransactionMode.SendsAtomicWithReceive;
    }
}