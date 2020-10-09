namespace NServiceBus.Core.Tests.DeliveryConstraintContextExtensions
{
    using System;
    using System.Collections.Generic;
    using Extensibility;
    using DelayedDelivery;
    using DeliveryConstraints;
    using NServiceBus.Features;
    using NServiceBus.Routing;
    using Settings;
    using Transport;
    using NUnit.Framework;

    [TestFixture]
    public class DeliveryConstraintContextExtensionsTests
    {
        [Test]
        public void Should_be_able_to_determine_if_delivery_constraint_is_supported()
        {
            var settings = new SettingsHolder();
            settings.Set(new FakeTransportInfrastructure());

            var context = new FeatureConfigurationContext(settings, null, null, null, null);
            var result = context.Settings.DoesTransportSupportConstraint<DeliveryConstraint>();
            Assert.IsTrue(result);
        }

        [Test]
        public void Should_be_able_to_try_remove_constraints()
        {
            var context = new ContextBag();

            var resultBeforeAdd = context.TryRemoveDeliveryConstraint(out DelayDeliveryWith _);

            context.AddDeliveryConstraint(new DelayDeliveryWith(TimeSpan.FromHours(1)));
            var resultAfterAdd = context.TryRemoveDeliveryConstraint(out DelayDeliveryWith _);

            Assert.IsFalse(resultBeforeAdd);
            Assert.IsTrue(resultAfterAdd);
        }

        class FakeTransportInfrastructure : TransportInfrastructure
        {
            public override EndpointAddress BuildLocalAddress(string queueName)
            {
                return new EndpointAddress(string.Empty, null, new Dictionary<string, string>(), null);
            }

            public override string ToTransportAddress(EndpointAddress logicalAddress)
            {
                throw new NotImplementedException();
            }

            public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure(ReceiveSettings receiveSettings)
            {
                throw new NotImplementedException();
            }

            public override TransportSendInfrastructure ConfigureSendInfrastructure()
            {
                throw new NotImplementedException();
            }

            public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure(SubscriptionSettings subscriptionSettings)
            {
                throw new NotImplementedException();
            }

            public override IEnumerable<Type> DeliveryConstraints { get; } = new[] { typeof(DelayDeliveryWith) };

            public override TransportTransactionMode TransactionMode { get; } = TransportTransactionMode.None;
        }
    }
}