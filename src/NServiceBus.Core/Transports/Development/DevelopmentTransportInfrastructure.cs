namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Performance.TimeToBeReceived;
    using Routing;
    using Settings;
    using Transport;

    class DevelopmentTransportInfrastructure : TransportInfrastructure
    {
        public DevelopmentTransportInfrastructure(SettingsHolder settings)
        {
            this.settings = settings;
        }

        public override IEnumerable<Type> DeliveryConstraints { get; } = new[]
        {
            typeof(DiscardIfNotReceivedBefore)
        };

        public override TransportTransactionMode TransactionMode => TransportTransactionMode.SendsAtomicWithReceive;

        public override OutboundRoutingPolicy OutboundRoutingPolicy { get; } = new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Multicast, OutboundRoutingType.Unicast);

        public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure()
        {
            return new TransportReceiveInfrastructure(() => new DevelopmentTransportMessagePump(), () => new DevelopmentTransportQueueCreator(), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override TransportSendInfrastructure ConfigureSendInfrastructure()
        {
            return new TransportSendInfrastructure(() => new DevelopmentTransportDispatcher(), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
        {
            return new TransportSubscriptionInfrastructure(() => new DevelopmentTransportSubscriptionManager(settings.EndpointName(), settings.LocalAddress()));
        }

        public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance)
        {
            return instance;
        }

        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            return Path.Combine(logicalAddress.EndpointInstance.Endpoint,
                logicalAddress.EndpointInstance.Discriminator ?? "",
                logicalAddress.Qualifier ?? "");
        }

        SettingsHolder settings;
    }
}