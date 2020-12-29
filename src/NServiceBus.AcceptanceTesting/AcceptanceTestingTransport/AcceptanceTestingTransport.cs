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

        public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses,
            CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(hostSettings), hostSettings);
            var infrastructure = new AcceptanceTestingTransportInfrastructure(hostSettings, this, receivers);
            infrastructure.ConfigureSendInfrastructure();

            await infrastructure.ConfigureReceiveInfrastructure().ConfigureAwait(false);

            //TODO: create queues
            /*
             * var queueCreator = transportReceiveInfrastructure.QueueCreatorFactory();
                        return queueCreator.CreateQueueIfNecessary(configuration.transportSeam.QueueBindings, identity);
             */
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
                TransportTransactionMode.SendsAtomicWithReceive
            };
        }
           
        public override bool SupportsDelayedDelivery { get; } = true;
        public override bool SupportsPublishSubscribe { get; } = true;
        public override bool SupportsTTBR { get; } = true;

        public string StorageLocation { get; set; }
    }
}