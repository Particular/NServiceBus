namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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

        [Obsolete("Obsolete marker to make the code compile", false)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        public override string ToTransportAddress(QueueAddress address)
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
        {
            var baseAddress = address.BaseAddress;
            ThrowForBadPath(baseAddress, "endpoint name");

            var discriminator = address.Discriminator;

            if (!string.IsNullOrEmpty(discriminator))
            {
                ThrowForBadPath(discriminator, "endpoint discriminator");

                baseAddress += "-" + discriminator;
            }

            var qualifier = address.Qualifier;

            if (!string.IsNullOrEmpty(qualifier))
            {
                ThrowForBadPath(qualifier, "address qualifier");

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

        static void ThrowForBadPath(string value, string valueName)
        {
            var invalidPathChars = Path.GetInvalidPathChars();

            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (value.IndexOfAny(invalidPathChars) < 0)
            {
                return;
            }

            throw new Exception($"The value for '{valueName}' has illegal path characters. Provided value: {value}. Must not contain any of {string.Join(", ", invalidPathChars)}.");
        }
    }
}