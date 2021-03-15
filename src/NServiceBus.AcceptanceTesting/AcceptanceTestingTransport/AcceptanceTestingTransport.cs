namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Routing;
    using Transport;

    public class AcceptanceTestingTransport : TransportDefinition, IMessageDrivenSubscriptionTransport
    {
        public AcceptanceTestingTransport(bool enableNativeDelayedDelivery = true, bool enableNativePublishSubscribe = true)
            : base(TransportTransactionMode.SendsAtomicWithReceive, enableNativeDelayedDelivery, enableNativePublishSubscribe, true)
        {
        }

        public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(hostSettings), hostSettings);

            var infrastructure = new AcceptanceTestingTransportInfrastructure(hostSettings, this, receivers);
            infrastructure.ConfigureDispatcher();
            await infrastructure.ConfigureReceivers().ConfigureAwait(false);

            return infrastructure;
        }

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes()
        {
            return new[]
            {
                TransportTransactionMode.None,
                TransportTransactionMode.ReceiveOnly,
                TransportTransactionMode.SendsAtomicWithReceive
            };
        }

        string storageLocation;
        public string StorageLocation
        {
            get => storageLocation;
            set
            {
                Guard.AgainstNull(nameof(StorageLocation), value);
                PathChecker.ThrowForBadPath(value, nameof(StorageLocation));
                storageLocation = value;
            }
        }
    }
}