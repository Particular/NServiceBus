namespace NServiceBus.Hosting.Tests
{
    using System;
    using NServiceBus.Features;
    using NUnit.Framework;

    [TestFixture]
    public class HostInfoSettingsTests
    {
        [Test]
        public void It_overrides_the_host_id()
        {
            var requestedId = Guid.NewGuid();
            var busConfig = new BusConfiguration();

            busConfig.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(requestedId);

            var configuredId = busConfig.Settings.Get<Guid>(UnicastBus.HostIdSettingsKey);
            Assert.AreEqual(requestedId, configuredId);
        }

        [Test]
        public void It_allows_to_generate_a_deterministic_id_using_instance_and_host_names()
        {
            var busConfig = new BusConfiguration();

            Assert.IsFalse(busConfig.Settings.HasSetting(UnicastBus.HostIdSettingsKey));

            busConfig.UniquelyIdentifyRunningInstance().UsingNames("Instance","Host");

            Assert.IsTrue(busConfig.Settings.HasExplicitValue(UnicastBus.HostIdSettingsKey));
        }
    }
}