﻿namespace NServiceBus.Core.Tests.Host;

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
        Assert.That(configuredId, Is.EqualTo(requestedId));
    }

    [Test]
    public void It_allows_to_generate_a_deterministic_id_using_instance_and_host_names()
    {
        var busConfig = new EndpointConfiguration("myendpoint");

        busConfig.UniquelyIdentifyRunningInstance().UsingNames("Instance", "Host");

        var configuredId = busConfig.Settings.Get<HostingComponent.Settings>().HostId;
        Assert.That(configuredId, Is.EqualTo(DeterministicGuid.Create("Instance", "Host")));
    }

    [Test]
    public void It_overrides_the_machine_name()
    {
        var busConfig = new EndpointConfiguration("myendpoint");

        busConfig.UniquelyIdentifyRunningInstance().UsingHostName("overridenhostname");

        var runtimeMachineName = RuntimeEnvironment.MachineName;
        Assert.That(runtimeMachineName, Is.EqualTo("overridenhostname"));

        var settingsMachineName = busConfig.Settings.Get<HostingComponent.Settings>().Properties["Machine"];
        Assert.That(settingsMachineName, Is.EqualTo("overridenhostname"));
    }
}