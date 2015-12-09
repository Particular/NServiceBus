namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Settings;
    using Transports;

    public class FakeTransport : TransportDefinition
    {
        protected override TransportReceivingConfigurationResult ConfigureForReceiving(TransportReceivingConfigurationContext context)
        {
            return new TransportReceivingConfigurationResult(() => new FakeReceiver(context.Settings.Get<Exception>()), () => new FakeQueueCreator(), () => Task.FromResult(StartupCheckResult.Success));
        }

        protected override TransportSendingConfigurationResult ConfigureForSending(TransportSendingConfigurationContext context)
        {
            return new TransportSendingConfigurationResult(() => new FakeDispatcher(), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override IEnumerable<Type> GetSupportedDeliveryConstraints()
        {
            return new List<Type>();
        }

        public override TransportTransactionMode GetSupportedTransactionMode()
        {
            return TransportTransactionMode.ReceiveOnly;
        }

        public override IManageSubscriptions GetSubscriptionManager()
        {
            throw new NotImplementedException();
        }

        public override string GetDiscriminatorForThisEndpointInstance(ReadOnlySettings settings)
        {
            return null;
        }

        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            return logicalAddress.ToString();
        }

        public override OutboundRoutingPolicy GetOutboundRoutingPolicy(ReadOnlySettings settings)
        {
            return new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Unicast, OutboundRoutingType.Unicast);
        }

        public override bool RequiresConnectionString => false;

        public override string ExampleConnectionStringForErrorMessage => null;
    }
}