namespace NServiceBus.Core.Tests.Routing.FileBasedDynamicRouting
{
    using System;
    using System.Linq;
    using NServiceBus.Features;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class FileRoutingTableFeatureTests
    {
        [Test]
        public void Should_configure_default_values()
        {
            var feature = new FileRoutingTableFeature();
            var settings = new SettingsHolder();

            feature.ConfigureDefaults(settings);

            Assert.That(settings.Get<string>(FileRoutingTableFeature.FilePathSettingsKey), Is.EqualTo("instance-mapping.xml"));
            Assert.That(settings.Get<TimeSpan>(FileRoutingTableFeature.CheckIntervalSettingsKey), Is.EqualTo(TimeSpan.FromSeconds(30)));
        }

        [Test]
        public void Requires_routing_enabled()
        {
            var feature = new FileRoutingTableFeature();

            Assert.That(feature.Dependencies.Any(dependencies => dependencies.Any(dependency => dependency == typeof(RoutingFeature).FullName)));
        }
    }
}