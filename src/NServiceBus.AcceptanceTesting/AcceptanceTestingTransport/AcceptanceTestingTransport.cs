using System.Threading.Tasks;

namespace NServiceBus
{
    using AcceptanceTesting;
    using Transport;

    public class AcceptanceTestingTransport : TransportDefinition
    {
        public string StorageDirectory { get; set; }

        public override async Task<TransportInfrastructure> Initialize(Transport.Settings settings, ReceiveSettings[] receiveSettings)
        {
            Guard.AgainstNull(nameof(settings), settings);

            var acceptanceTestingTransportInfrastructure = new AcceptanceTestingTransportInfrastructure(receiveSettings, settings, this);
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

        public override TransportTransactionMode MaxSupportedTransactionMode => TransportTransactionMode.SendsAtomicWithReceive;

        /// <summary>
        /// 
        /// </summary>
        public override bool SupportsTTBR { get; } = true;

    }
}