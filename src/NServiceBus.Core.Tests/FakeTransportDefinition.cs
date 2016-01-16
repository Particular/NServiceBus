namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    public class FakeTransportDefinition : TransportDefinition
    {
        public override bool RequiresConnectionString => false;

        protected internal override TransportReceivingConfigurationResult ConfigureForReceiving(TransportReceivingConfigurationContext context)
        {
            throw new NotImplementedException();
        }

        protected internal override TransportSendingConfigurationResult ConfigureForSending(TransportSendingConfigurationContext context)
        {
            return new TransportSendingConfigurationResult(() => null, () => Task.FromResult(StartupCheckResult.Success));
        }

        public override IEnumerable<Type> GetSupportedDeliveryConstraints()
        {
            yield return typeof(DelayDeliveryWith);
        }

        public override TransportTransactionMode GetSupportedTransactionMode()
        {
            return TransportTransactionMode.None;
        }

        public override IManageSubscriptions GetSubscriptionManager()
        {
            throw new NotImplementedException();
        }

        public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance, ReadOnlySettings settings)
        {
            return instance;
        }

        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            return logicalAddress.ToString();
        }

        public override OutboundRoutingPolicy GetOutboundRoutingPolicy(ReadOnlySettings settings)
        {
            return new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Unicast, OutboundRoutingType.Unicast);
        }

        public override string ExampleConnectionStringForErrorMessage { get; } = "";
    }
}