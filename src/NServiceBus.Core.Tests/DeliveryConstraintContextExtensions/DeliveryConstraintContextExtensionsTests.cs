﻿namespace NServiceBus.Core.Tests.DeliveryConstraintContextExtensions
{
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

            var result = context.DoesTransportSupportConstraint<DeliveryConstraint>();
            Assert.IsTrue(result);
        }
    }
}