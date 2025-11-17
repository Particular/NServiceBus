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

        var configuration = new AssemblyScanningComponent.Configuration(settingsHolder)
        {
            AssemblyScannerConfiguration =
            {
                AdditionalAssemblyScanningPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "Nested", "Subfolder")
            }
        };

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

        Assert.That(exception.Message, Does.Contain($"Assembly scanning has been disabled. This prevents messages, message handlers, features and other functionality from loading correctly. Enable {nameof(AssemblyScannerConfiguration.ScanAppDomainAssemblies)} or {nameof(AssemblyScannerConfiguration.ScanFileSystemAssemblies)}"));
    }
}