namespace NServiceBus
{
    using AcceptanceTesting;
    using Routing;
    using Settings;
    using Transport;

    public class AcceptanceTestingTransport : TransportDefinition, IMessageDrivenSubscriptionTransport
    {
        public string StorageDirectory { get; set; }

        public override TransportInfrastructure Initialize(TransportSettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);

            return new AcceptanceTestingTransportInfrastructure(settings, this);
        }
    }
}