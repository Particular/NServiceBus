namespace NServiceBus.Core.Tests.DeliveryConstraintContextExtensions
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Features;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class DeliveryConstraintContextExtensionsTests
    {
        [Test]
        public void Should_be_able_to_determine_if_delivery_constraint_is_supported()
        {
            var settings = new SettingsHolder();
            var fakeTransportDefinition = new FakeTransportDefinition();
            settings.Set<TransportDefinition>(fakeTransportDefinition);
            settings.Set<TransportInfrastructure>(fakeTransportDefinition.Initialize(settings, null));

            var context = new FeatureConfigurationContext(settings, null, null);
            var result = context.DoesTransportSupportConstraint<DeliveryConstraint>();
            Assert.IsTrue(result);
        }

        class FakeTransportDefinition : TransportDefinition
        {
            protected internal override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
            {
                return new FakeTransportInfrastructure();
            }

            public override string ExampleConnectionStringForErrorMessage { get; } = string.Empty;
        }

        class FakeTransportInfrastructure : TransportInfrastructure
        {

            public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance)
            {
                throw new NotImplementedException();
            }

            public override string ToTransportAddress(LogicalAddress logicalAddress)
            {
                throw new NotImplementedException();
            }

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

            public override IEnumerable<Type> DeliveryConstraints { get; } = new[] { typeof(DelayDeliveryWith) };

            public override TransportTransactionMode TransactionMode { get; } = TransportTransactionMode.None;

            public override OutboundRoutingPolicy OutboundRoutingPolicy { get; } = new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Unicast, OutboundRoutingType.Unicast);

            
        }
    }
}