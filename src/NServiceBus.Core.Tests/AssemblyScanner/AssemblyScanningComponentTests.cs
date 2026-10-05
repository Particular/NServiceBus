#nullable enable

namespace NServiceBus.Core.Tests.AssemblyScanner;

using System;
using System.IO;
using System.Linq;
using Settings;
using NUnit.Framework;

[TestFixture]
class AssemblyScanningComponentTests
{
    [Test]
    public void Should_initialize_scanner_with_custom_path_when_provided()
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set(new HostingComponent.Settings(settingsHolder));

        var configuration = new AssemblyScanningComponent.Configuration(settingsHolder) { AssemblyScannerConfiguration = { AdditionalAssemblyScanningPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "Nested", "Subfolder") } };

        var component = AssemblyScanningComponent.Initialize(configuration, settingsHolder);

        var foundTypeFromScannedPath = component.AvailableTypes.Any(x => x.Name == "NestedClass");

        Assert.That(foundTypeFromScannedPath, Is.True, "Was expected to scan a custom path, but 'nested.dll' was not scanned.");
    }

    [Test]
    public void Should_throw_when_both_file_and_appdomain_scanning_turned_off()
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set(new HostingComponent.Settings(settingsHolder));

        var configuration = new AssemblyScanningComponent.Configuration(settingsHolder)
        {
            AssemblyScannerConfiguration =
            {
                ScanFileSystemAssemblies = false,
                ScanAppDomainAssemblies = false
            }
        };

        var exception = Assert.Throws<Exception>(() => AssemblyScanningComponent.Initialize(configuration, settingsHolder));

        Assert.That(exception?.Message, Does.Contain("Both file and appdomain scanning has been turned off"));
    }

    [Test]
    public void Should_allow_assembly_scanning_to_be_disabled()
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set(new HostingComponent.Settings(settingsHolder));

        var configuration = new AssemblyScanningComponent.Configuration(settingsHolder) { AssemblyScannerConfiguration = { Disable = true } };

        var component = AssemblyScanningComponent.Initialize(configuration, settingsHolder);

        Assert.That(component.AvailableTypes, Is.Empty);
    }

    [Test]
    public void Should_enable_strict_registered_only_mode_when_scanning_disabled_and_dynamic_code_not_supported()
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set(new HostingComponent.Settings(settingsHolder));

        var configuration = new AssemblyScanningComponent.Configuration(settingsHolder)
        {
            DynamicCodeSupported = false,
            AssemblyScannerConfiguration = { Disable = true }
        };

        var component = AssemblyScanningComponent.Initialize(configuration, settingsHolder);

        Assert.That(component.IsStrictRegisteredOnlyMode, Is.True);
    }

    [Test]
    public void Should_enable_strict_registered_only_mode_when_scanning_disabled_and_feature_switch_enabled()
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set(new HostingComponent.Settings(settingsHolder));

        var configuration = new AssemblyScanningComponent.Configuration(settingsHolder)
        {
            StrictRegisteredOnlyMessageMetadataEnabled = true,
            AssemblyScannerConfiguration = { Disable = true }
        };

        var component = AssemblyScanningComponent.Initialize(configuration, settingsHolder);

        Assert.That(component.IsStrictRegisteredOnlyMode, Is.True);
    }

    [Test]
    public void Should_enable_strict_registered_only_mode_when_scanning_disabled_and_runtime_switch_enabled()
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set(new HostingComponent.Settings(settingsHolder));

        using (AppContextSwitchHelper.Enable(AppContextSwitches.StrictRegisteredOnlyMessageMetadataSwitchName))
        {
            var configuration = new AssemblyScanningComponent.Configuration(settingsHolder) { AssemblyScannerConfiguration = { Disable = true } };

            var component = AssemblyScanningComponent.Initialize(configuration, settingsHolder);

            Assert.That(component.IsStrictRegisteredOnlyMode, Is.True);
        }
    }

    [Test]
    public void Should_not_enable_strict_registered_only_mode_when_scanning_disabled_in_normal_jit()
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set(new HostingComponent.Settings(settingsHolder));

        var configuration = new AssemblyScanningComponent.Configuration(settingsHolder) { AssemblyScannerConfiguration = { Disable = true } };

        var component = AssemblyScanningComponent.Initialize(configuration, settingsHolder);

        Assert.That(component.IsStrictRegisteredOnlyMode, Is.False);
    }

    [Test]
    public void Should_not_enable_strict_registered_only_mode_when_scanning_enabled_and_feature_switch_enabled()
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set(new HostingComponent.Settings(settingsHolder));

        var configuration = new AssemblyScanningComponent.Configuration(settingsHolder)
        {
            StrictRegisteredOnlyMessageMetadataEnabled = true,
            DynamicCodeSupported = true
        };

        var component = AssemblyScanningComponent.Initialize(configuration, settingsHolder);

        // Strict mode is only ever enabled when assembly scanning is disabled. With scanning enabled the
        // component must not enable it even when the feature switch is enabled.
        Assert.That(component.IsStrictRegisteredOnlyMode, Is.False);
    }

    [Test]
    public void Should_throw_enabled_and_dynamic_code_not_supported()
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set(new HostingComponent.Settings(settingsHolder));

        var configuration = new AssemblyScanningComponent.Configuration(settingsHolder) { DynamicCodeSupported = false };

        var exception = Assert.Throws<Exception>(() => AssemblyScanningComponent.Initialize(configuration, settingsHolder));

        Assert.That(exception?.Message, Does.Contain("Assembly scanning requires to access unreferenced and dynamic code"));
    }

    sealed class AppContextSwitchHelper : IDisposable
    {
        readonly string switchName;

        public static AppContextSwitchHelper Enable(string switchName) => new(switchName, true);

        AppContextSwitchHelper(string switchName, bool value)
        {
            this.switchName = switchName;
            AppContext.SetSwitch(switchName, value);
            AppContextSwitches.ResetStrictRegisteredOnlyMessageMetadata();
        }

        public void Dispose()
        {
            AppContext.SetSwitch(switchName, false);
            AppContextSwitches.ResetStrictRegisteredOnlyMessageMetadata();
        }
    }
}