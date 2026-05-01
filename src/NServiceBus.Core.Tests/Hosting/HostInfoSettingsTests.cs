namespace NServiceBus.Core.Tests.Host;

using System;
using NServiceBus.Support;
using NServiceBus.Utils;
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
        Assert.That(configuredId, Is.EqualTo(LegacyDeterministicGuid.Create("Instance", "Host")));
    }

    [Test]
    public void UsingNames_with_v2_switch_uses_deterministic_guid()
    {
        using (AppContextSwitchHelper.Enable("NServiceBus.Core.Hosting.UseV2DeterministicGuid"))
        {
            var busConfig = new EndpointConfiguration("myendpoint");

            busConfig.UniquelyIdentifyRunningInstance().UsingNames("Instance", "Host");

            var configuredId = busConfig.Settings.Get<HostingComponent.Settings>().HostId;
            Assert.That(configuredId, Is.EqualTo(DeterministicGuid.Create("Instance", "Host")));
        }
    }

    [Test]
    public void UsingNames_with_v2_switch_explicitly_disabled_uses_legacy_deterministic_guid()
    {
        using (AppContextSwitchHelper.Disable("NServiceBus.Core.Hosting.UseV2DeterministicGuid"))
        {
            var busConfig = new EndpointConfiguration("myendpoint");

            busConfig.UniquelyIdentifyRunningInstance().UsingNames("Instance", "Host");

            var configuredId = busConfig.Settings.Get<HostingComponent.Settings>().HostId;
            Assert.That(configuredId, Is.EqualTo(LegacyDeterministicGuid.Create("Instance", "Host")));
        }
    }

    [Test]
    public void GenerateHostId_without_v2_switch_sets_legacy_flag()
    {
        var busConfig = new EndpointConfiguration("myendpoint");

        busConfig.UniquelyIdentifyRunningInstance().UsingNames("Instance", "Host");

        var settings = busConfig.Settings;
        Assert.That(settings.Get<bool>(AppContextSwitches.UsedLegacyDeterministicGuidSettingsKey), Is.True);
    }

    [Test]
    public void GenerateHostId_with_v2_switch_does_not_set_legacy_flag()
    {
        using (AppContextSwitchHelper.Enable("NServiceBus.Core.Hosting.UseV2DeterministicGuid"))
        {
            var busConfig = new EndpointConfiguration("myendpoint");

            busConfig.UniquelyIdentifyRunningInstance().UsingNames("Instance", "Host");

            Assert.That(
                busConfig.Settings.GetOrDefault<bool>(AppContextSwitches.UsedLegacyDeterministicGuidSettingsKey),
                Is.False);
        }
    }

    [Test]
    public void UsingCustomIdentifier_does_not_set_legacy_flag()
    {
        var busConfig = new EndpointConfiguration("myendpoint");

        busConfig.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(Guid.NewGuid());

        Assert.That(
            busConfig.Settings.GetOrDefault<bool>(AppContextSwitches.UsedLegacyDeterministicGuidSettingsKey),
            Is.False);
    }

    [Test]
    public void Default_host_id_without_v2_switch_uses_legacy()
    {
        var busConfig = new EndpointConfiguration("myendpoint");
        var settings = busConfig.Settings.Get<HostingComponent.Settings>();

        settings.ApplyHostIdDefaultIfNeeded();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.HostId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(busConfig.Settings.Get<bool>(AppContextSwitches.UsedLegacyDeterministicGuidSettingsKey), Is.True);
        }
    }

    [Test]
    public void Default_host_id_with_v2_switch_uses_deterministic_guid()
    {
        using (AppContextSwitchHelper.Enable("NServiceBus.Core.Hosting.UseV2DeterministicGuid"))
        {
            var busConfig = new EndpointConfiguration("myendpoint");
            var settings = busConfig.Settings.Get<HostingComponent.Settings>();

            settings.ApplyHostIdDefaultIfNeeded();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.HostId, Is.Not.EqualTo(Guid.Empty));
                Assert.That(
                    busConfig.Settings.GetOrDefault<bool>(AppContextSwitches.UsedLegacyDeterministicGuidSettingsKey),
                    Is.False);
            }
        }
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

    sealed class AppContextSwitchHelper : IDisposable
    {
        readonly string switchName;

        public static AppContextSwitchHelper Enable(string switchName) => new(switchName, true);

        public static AppContextSwitchHelper Disable(string switchName) => new(switchName, false);

        AppContextSwitchHelper(string switchName, bool value)
        {
            this.switchName = switchName;
            AppContext.SetSwitch(switchName, value);
            AppContextSwitches.ResetUseV2DeterministicGuid();
        }

        public void Dispose()
        {
            AppContext.SetSwitch(switchName, false);
            AppContextSwitches.ResetUseV2DeterministicGuid();
        }
    }
}