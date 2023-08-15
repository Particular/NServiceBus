namespace NServiceBus
{
    using System;
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
            Guard.ThrowIfNull(hostSettings);

            var infrastructure = new AcceptanceTestingTransportInfrastructure(hostSettings, this, receivers);
            infrastructure.ConfigureDispatcher();
            await infrastructure.ConfigureReceivers().ConfigureAwait(false);

            return infrastructure;
        }

        [Obsolete("This should be removed when TransportDefinition.ToTransportAddress is removed in v10.", true)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        public override string ToTransportAddress(QueueAddress address) => throw new NotImplementedException();
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member

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
                Guard.ThrowIfNull(value);
                PathChecker.ThrowForBadPath(value, nameof(StorageLocation));
                storageLocation = value;
            }
        }
    }
}