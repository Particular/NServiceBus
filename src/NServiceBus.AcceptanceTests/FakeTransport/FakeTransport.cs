namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System;
    using System.Collections.Generic;
    using Settings;
    using Transports;

    public class FakeTransport : TransportDefinition
    {
        public override IEnumerable<Type> GetSupportedDeliveryConstraints()
        {
            return new List<Type>();
        }

        public override TransactionSupport GetTransactionSupport()
        {
            return TransactionSupport.SingleQueue;
        }

        public override IManageSubscriptions GetSubscriptionManager()
        {
            throw new NotImplementedException();
        }

        public override string GetDiscriminatorForThisEndpointInstance()
        {
            return null;
        }

        protected override void Configure(BusConfiguration config)
        {
            config.EnableFeature<FakeTransportConfigurator>();
        }

        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            return logicalAddress.ToString();
        }

        public override OutboundRoutingPolicy GetOutboundRoutingPolicy(ReadOnlySettings settings)
        {
            return new OutboundRoutingPolicy(OutboundRoutingType.DirectSend, OutboundRoutingType.DirectSend, OutboundRoutingType.DirectSend);
        }
    }
}