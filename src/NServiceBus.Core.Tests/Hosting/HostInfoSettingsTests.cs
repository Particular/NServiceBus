namespace NServiceBus.Core.Tests.Host
{
    using System;
    using NServiceBus.Support;
    using NUnit.Framework;

    [TestFixture]
    public class HostInfoSettingsTests
    {
        [Test]
        public void It_overrides_the_host_id()
        {
            var requestedId = Guid.NewGuid();
            var busConfig = new EndpointConfiguration("myendpoint");

            busConfig.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(requestedId);

            var configuredId = busConfig.Settings.Get<HostingComponent.Settings>().HostId;
            Assert.AreEqual(requestedId, configuredId);
        }

        [Test]
        public void It_allows_to_generate_a_deterministic_id_using_instance_and_host_names()
        {
            var busConfig = new EndpointConfiguration("myendpoint");

            busConfig.UniquelyIdentifyRunningInstance().UsingNames("Instance", "Host");

            var configuredId = busConfig.Settings.Get<HostingComponent.Settings>().HostId;
            Assert.AreEqual(DeterministicGuid.Create("Instance", "Host"), configuredId);
        }

        [Test]
        public void It_overrides_the_machine_name()
        {
            var busConfig = new EndpointConfiguration("myendpoint");

            busConfig.UniquelyIdentifyRunningInstance().OverrideHostName("overridenhostname");

            var hostName = RuntimeEnvironment.MachineName;
            Assert.AreEqual("overridenhostname", hostName);
        }
    }
}