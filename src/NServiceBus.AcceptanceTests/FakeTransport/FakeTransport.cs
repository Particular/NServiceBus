namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.Transports;

    public class FakeTransport : TransportDefinition
    {
        protected override void Configure(BusConfiguration config)
        {
            config.EnableFeature<FakeTransportConfigurator>();
        }

        public override string GetSubScope(string address, string qualifier)
        {
            return address + "#" + qualifier;
        }

        public override IEnumerable<Type> GetSupportedDeliveryConstraints()
        {
            return new List<Type>();
        }

        public override ConsistencyGuarantee GetDefaultConsistencyGuarantee()
        {
            return new AtLeastOnce();
        }

        public override IManageSubscriptions GetSubscriptionManager()
        {
            throw new NotImplementedException();
        }
    }
}