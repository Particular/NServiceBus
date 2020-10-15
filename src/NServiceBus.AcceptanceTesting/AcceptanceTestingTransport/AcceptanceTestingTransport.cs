using System.Threading.Tasks;

namespace NServiceBus
{
    using AcceptanceTesting;
    using Transport;

    public class AcceptanceTestingTransport : TransportDefinition
    {
        public string StorageDirectory { get; set; }

        public override async Task<TransportInfrastructure> Initialize(Transport.Settings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);

            var acceptanceTestingTransportInfrastructure = new AcceptanceTestingTransportInfrastructure(settings, this);
            acceptanceTestingTransportInfrastructure.ConfigureSendInfrastructure();
            return acceptanceTestingTransportInfrastructure;
        }
    }
}