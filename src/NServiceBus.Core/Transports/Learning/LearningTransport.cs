namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    /// <summary>
    /// A transport optimized for development and learning use. DO NOT use in production.
    /// </summary>
    public class LearningTransport : TransportDefinition
    {
        /// <summary>
        /// Creates a new instance of a learning transport.
        /// </summary>
        public LearningTransport()
            : base(TransportTransactionMode.SendsAtomicWithReceive, supportsDelayedDelivery: true, supportsPublishSubscribe: true, supportsTTBR: true)
        {
        }

        /// <summary>
        /// Initializes all the factories and supported features for the transport. This method is called right before all features
        /// are activated and the settings will be locked down. This means you can use the SettingsHolder both for providing
        /// default capabilities as well as for initializing the transport's configuration based on those settings (the user cannot
        /// provide information anymore at this stage).
        /// </summary>
        public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(hostSettings), hostSettings);
            var learningTransportInfrastructure = new LearningTransportInfrastructure(hostSettings, this, receivers);
            learningTransportInfrastructure.ConfigureSendInfrastructure();

            learningTransportInfrastructure.ConfigureReceiveInfrastructure();

            return Task.FromResult<TransportInfrastructure>(learningTransportInfrastructure);
        }

        /// <inheritdoc />
        public override Task<string> ToTransportAddress(QueueAddress queueAddress, CancellationToken cancellationToken = default)
        {
            var address = queueAddress.BaseAddress;
            PathChecker.ThrowForBadPath(address, "endpoint name");

            var discriminator = queueAddress.Discriminator;

            if (!string.IsNullOrEmpty(discriminator))
            {
                PathChecker.ThrowForBadPath(discriminator, "endpoint discriminator");

                address += "-" + discriminator;
            }

            var qualifier = queueAddress.Qualifier;

            if (!string.IsNullOrEmpty(qualifier))
            {
                PathChecker.ThrowForBadPath(qualifier, "address qualifier");

                address += "-" + qualifier;
            }

            return Task.FromResult(address);
        }

        /// <inheritdoc />
        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes()
        {
            return new[]
            {
                TransportTransactionMode.None,
                TransportTransactionMode.ReceiveOnly,
                TransportTransactionMode.SendsAtomicWithReceive
            };
        }

        /// <summary>
        /// Configures the storage directory to store files created by the transport.
        /// </summary>
        public string StorageDirectory { get; set; }

        /// <summary>
        /// If set to true, limits the message maximum payload size to 64 kilobytes.
        /// </summary>
        public bool RestrictPayloadSize { get; set; } = true;
    }
}