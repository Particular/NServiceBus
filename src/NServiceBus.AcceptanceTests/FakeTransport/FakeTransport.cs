namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System;
    using System.Collections.Generic;
    using Settings;
    using Transports;

    public class FakeTransport : TransportDefinition
    {
        protected override void ConfigureForReceiving(TransportReceivingConfigurationContext context)
        {
            context.SetMessagePumpFactory(c => new FakeReceiver(c, context.Settings.Get<Exception>()));
            context.SetQueueCreatorFactory(() => new FakeQueueCreator());
        }

        protected override void ConfigureForSending(TransportSendingConfigurationContext context)
        {
            context.SetDispatcherFactory(() => new FakeDispatcher());
        }

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

        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            return logicalAddress.ToString();
        }

        public override OutboundRoutingPolicy GetOutboundRoutingPolicy(ReadOnlySettings settings)
        {
            return new OutboundRoutingPolicy(OutboundRoutingType.DirectSend, OutboundRoutingType.DirectSend, OutboundRoutingType.DirectSend);
        }

        public override bool RequiresConnectionString => false;

        public override string ExampleConnectionStringForErrorMessage => null;
    }
}