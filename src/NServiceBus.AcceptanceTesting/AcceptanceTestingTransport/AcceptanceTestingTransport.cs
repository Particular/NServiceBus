namespace NServiceBus
{
    using AcceptanceTesting;
    using Transport;

    public class AcceptanceTestingTransport : TransportDefinition
    {
        public string StorageDirectory { get; set; }

        public override TransportInfrastructure Initialize(TransportSettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);

            return new AcceptanceTestingTransportInfrastructure(settings, this);
        }
    }
}