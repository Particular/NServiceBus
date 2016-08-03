namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using Settings;
    using Transport;

    public class FakeTransportInfrastructure : TransportInfrastructure
    {
        public FakeTransportInfrastructure(ReadOnlySettings settings)
        {
            this.settings = settings;
        }

        public override IEnumerable<Type> DeliveryConstraints { get; } = Enumerable.Empty<Type>();

        public override TransportTransactionMode TransactionMode
        {
            get
            {
                TransportTransactionMode supportedTransactionMode;

                if (settings.TryGet("FakeTransport.SupportedTransactionMode", out supportedTransactionMode))
                {
                    return supportedTransactionMode;
                }

                return TransportTransactionMode.TransactionScope;
            }
        }

        public override OutboundRoutingPolicy OutboundRoutingPolicy { get; } = new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Unicast, OutboundRoutingType.Unicast);

        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            return logicalAddress.ToString();
        }

        public override string ToTransportAddress(EndpointInstance endpointInstance)
        {
            return endpointInstance.ToString();
        }

        public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure()
        {
            return new TransportReceiveInfrastructure(() => new FakeReceiver(settings.GetOrDefault<Exception>()), () => new FakeQueueCreator(), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override TransportSendInfrastructure ConfigureSendInfrastructure()
        {
            return new TransportSendInfrastructure(() => new FakeDispatcher(), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
        {
            throw new NotImplementedException();
        }

        ReadOnlySettings settings;
    }
}