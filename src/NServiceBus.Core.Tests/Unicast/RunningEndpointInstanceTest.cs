﻿namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Features;
    using NServiceBus.Transport;
    using NUnit.Framework;
    using Routing;
    using Settings;
    using Testing;

    [TestFixture]
    public class RunningEndpointInstanceTest
    {
        [Test]
        public async Task ShouldAllowMultipleStops()
        {
            var testee = new RunningEndpointInstance(
                new SettingsHolder(),
                new FakeBuilder(),
                null,
                new FeatureRunner(new FeatureActivator(new SettingsHolder())),
                new MessageSession(new RootContext(null, null, null, null)), new FakeTransportInfrastructure());

            await testee.Stop();

            Assert.That(async () => await testee.Stop(), Throws.Nothing);
        }

        class FakeTransportInfrastructure : TransportInfrastructure
        {
            public override IEnumerable<Type> DeliveryConstraints { get; }
            public override TransportTransactionMode TransactionMode { get; }
            public override OutboundRoutingPolicy OutboundRoutingPolicy { get; }

            public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure()
            {
                throw new NotImplementedException();
            }

            public override TransportSendInfrastructure ConfigureSendInfrastructure()
            {
                throw new NotImplementedException();
            }

            public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
            {
                throw new NotImplementedException();
            }

            public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance)
            {
                throw new NotImplementedException();
            }

            public override string ToTransportAddress(LogicalAddress logicalAddress)
            {
                throw new NotImplementedException();
            }
        }
    }
}