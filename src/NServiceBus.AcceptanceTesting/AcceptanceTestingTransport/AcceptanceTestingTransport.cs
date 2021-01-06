using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NServiceBus
{
    using AcceptanceTesting;
    using Routing;
    using Transport;

    public class AcceptanceTestingTransport : TransportDefinition, IMessageDrivenSubscriptionTransport
    {
        public AcceptanceTestingTransport() : base(TransportTransactionMode.SendsAtomicWithReceive)
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

        public override string ToTransportAddress(QueueAddress address)
        {
            var baseAddress = address.BaseAddress;
            PathChecker.ThrowForBadPath(baseAddress, "endpoint name");

            var discriminator = address.Discriminator;

            if (!string.IsNullOrEmpty(discriminator))
            {
                PathChecker.ThrowForBadPath(discriminator, "endpoint discriminator");

                baseAddress += "-" + discriminator;
            }

            var qualifier = address.Qualifier;

            if (!string.IsNullOrEmpty(qualifier))
            {
                PathChecker.ThrowForBadPath(qualifier, "address qualifier");

                baseAddress += "-" + qualifier;
            }

            return baseAddress;
        }

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes()
        {
            return  new[]
            {
                TransportTransactionMode.None,
                TransportTransactionMode.ReceiveOnly,
                TransportTransactionMode.SendsAtomicWithReceive
            };
        }

        public override bool SupportsDelayedDelivery => EnableNativeDelayedDeliery;
        public bool EnableNativeDelayedDeliery { get; set; } = true;

        public override bool SupportsPublishSubscribe => EnableNativePublishSubscribe;
        public bool EnableNativePublishSubscribe { get; set; } = true;

        public override bool SupportsTTBR { get; } = true;

        private string storageLocation;
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