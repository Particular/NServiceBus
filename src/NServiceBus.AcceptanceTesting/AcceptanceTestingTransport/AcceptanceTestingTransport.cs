using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NServiceBus
{
    using AcceptanceTesting;
    using Transport;

    public class AcceptanceTestingTransport : TransportDefinition
    {
        public string StorageDirectory { get; set; }

        public override async Task<TransportInfrastructure> Initialize(Transport.Settings settings, ReceiveSettings[] receivers, string[] SendingAddresses, CancellationToken cancellationToken)
        {
            Guard.AgainstNull(nameof(settings), settings);

            var acceptanceTestingTransportInfrastructure = new AcceptanceTestingTransportInfrastructure(receivers, settings, this);
            acceptanceTestingTransportInfrastructure.ConfigureSendInfrastructure();

            await acceptanceTestingTransportInfrastructure.ConfigureReceiveInfrastructure().ConfigureAwait(false);
            return acceptanceTestingTransportInfrastructure;
        }

        public override string ToTransportAddress(EndpointAddress logicalAddress)
        {
            var address = logicalAddress.Endpoint;
            PathChecker.ThrowForBadPath(address, "endpoint name");

            var discriminator = logicalAddress.Discriminator;

            if (!string.IsNullOrEmpty(discriminator))
            {
                PathChecker.ThrowForBadPath(discriminator, "endpoint discriminator");

                address += "-" + discriminator;
            }

            var qualifier = logicalAddress.Qualifier;

            if (!string.IsNullOrEmpty(qualifier))
            {
                PathChecker.ThrowForBadPath(qualifier, "address qualifier");

                address += "-" + qualifier;
            }

            return address;
        }

        public override IReadOnlyCollection<TransportTransactionMode> SupportedTransactionModes { get; protected set; } =
            new[]
            {
                TransportTransactionMode.None,
                TransportTransactionMode.ReceiveOnly,
                TransportTransactionMode.SendsAtomicWithReceive
            };

        /// <summary>
        /// </summary>
        public override bool SupportsTTBR { get; } = true;

        /// <summary>
        /// </summary>
        public override bool SupportsDelayedDelivery { get; } = true;

        /// <summary>
        /// </summary>
        public override bool SupportsPublishSubscribe { get; } = true;
    }
}