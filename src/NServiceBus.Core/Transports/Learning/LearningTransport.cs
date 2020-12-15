using System.Collections.Generic;
using System.Threading;
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
        /// Initializes all the factories and supported features for the transport. This method is called right before all features
        /// are activated and the settings will be locked down. This means you can use the SettingsHolder both for providing
        /// default capabilities as well as for initializing the transport's configuration based on those settings (the user cannot
        /// provide information anymore at this stage).
        /// </summary>
        public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses,
            CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(hostSettings), hostSettings);
            var learningTransportInfrastructure = new LearningTransportInfrastructure(hostSettings, this, receivers);
            learningTransportInfrastructure.ConfigureSendInfrastructure();

            await learningTransportInfrastructure.ConfigureReceiveInfrastructure().ConfigureAwait(false);

            return learningTransportInfrastructure;
        }

        /// <summary>
        /// </summary>
        public override string ToTransportAddress(QueueAddress queueAddress)
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

            return address;
        }

        /// <summary>
        /// </summary>
        public override IReadOnlyCollection<TransportTransactionMode> SupportedTransactionModes { get; protected set; } = 
            new[] 
            {
                TransportTransactionMode.None,
                TransportTransactionMode.ReceiveOnly,
                TransportTransactionMode.SendsAtomicWithReceive,
                TransportTransactionMode.TransactionScope
            };

        
        /// <summary>
        /// </summary>
        public override bool SupportsDelayedDelivery { get; } = true;

        /// <summary>
        /// </summary>
        public override bool SupportsPublishSubscribe { get; } = true;

        /// <summary>
        /// </summary>
        public string StorageDirectory { get; set; }

        /// <summary>
        /// </summary>
        public bool RestrictPayloadSize { get; set; }
    }
}