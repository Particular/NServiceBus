namespace NServiceBus.Core.Tests.DeliveryConstraintContextExtensions
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Features;
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
            settings.Set<TransportDefinition>(new FakeTransportDefinition());
            var context = new FeatureConfigurationContext(settings, null, null);
            var result = DeliveryConstraintContextExtensions.DoesTransportSupportConstraint<DeliveryConstraint>(context);
            Assert.IsTrue(result);
        }

        class FakeTransportDefinition : TransportDefinition
        {
            protected internal override TransportReceivingConfigurationResult ConfigureForReceiving(TransportReceivingConfigurationContext context)
            {
                throw new NotImplementedException();
            }

            protected internal override TransportSendingConfigurationResult ConfigureForSending(TransportSendingConfigurationContext context)
            {
                throw new NotImplementedException();
            }

            public override IEnumerable<Type> GetSupportedDeliveryConstraints()
            {
                yield return typeof(DelayDeliveryWith);
            }

            public override TransactionSupport GetTransactionSupport()
            {
                throw new NotImplementedException();
            }

            public override IManageSubscriptions GetSubscriptionManager()
            {
                throw new NotImplementedException();
            }

            public override string GetDiscriminatorForThisEndpointInstance(ReadOnlySettings settings)
            {
                throw new NotImplementedException();
            }

            public override string ToTransportAddress(LogicalAddress logicalAddress)
            {
                throw new NotImplementedException();
            }

            public override OutboundRoutingPolicy GetOutboundRoutingPolicy(ReadOnlySettings settings)
            {
                throw new NotImplementedException();
            }

            public override string ExampleConnectionStringForErrorMessage { get; } = "";
        }
    }
}