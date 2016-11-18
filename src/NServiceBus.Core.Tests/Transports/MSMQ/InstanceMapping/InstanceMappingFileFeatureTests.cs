namespace NServiceBus.Core.Tests.Routing.FileBasedDynamicRouting
{
    using System;
    using NServiceBus.Features;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class InstanceMappingFileFeatureTests
    {
        [Test]
        public void Should_configure_default_values()
        {
            var feature = new InstanceMappingFileFeature();
            var settings = new SettingsHolder();

            feature.ConfigureDefaults(settings);

            Assert.That(settings.Get<string>(InstanceMappingFileFeature.FilePathSettingsKey), Is.EqualTo("instance-mapping.xml"));
            Assert.That(settings.Get<TimeSpan>(InstanceMappingFileFeature.CheckIntervalSettingsKey), Is.EqualTo(TimeSpan.FromSeconds(30)));
        }
    }
}