﻿namespace NServiceBus.Core.Tests.Host
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
            var busConfig = new EndpointConfiguration("myendpoint");

            busConfig.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(requestedId);

            var configuredId = busConfig.Settings.Get<Guid>(HostInformationFeature.HostIdSettingsKey);
            Assert.AreEqual(requestedId, configuredId);
        }

        [Test]
        public void It_allows_to_generate_a_deterministic_id_using_instance_and_host_names()
        {
            var busConfig = new EndpointConfiguration("myendpoint");

            Assert.IsFalse(busConfig.Settings.HasSetting(HostInformationFeature.HostIdSettingsKey));

            busConfig.UniquelyIdentifyRunningInstance().UsingNames("Instance","Host");

            Assert.IsTrue(busConfig.Settings.HasExplicitValue(HostInformationFeature.HostIdSettingsKey));
        }
    }
}