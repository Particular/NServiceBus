namespace NServiceBus
{
    using System.Collections.Generic;
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

        public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses)
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