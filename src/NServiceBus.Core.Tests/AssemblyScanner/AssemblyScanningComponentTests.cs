namespace NServiceBus.Core.Tests.AssemblyScanner
{
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

            var configuration = new AssemblyScanningComponent.Configuration(settingsHolder);

            configuration.AssemblyScannerConfiguration.CustomAssemblyScanningPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "Nested", "Subfolder");

            var component = AssemblyScanningComponent.Initialize(configuration, settingsHolder);

            var foundTypeFromScannedPath = component.AvailableTypes.Any(x => x.Name == "NestedClass");

            Assert.True(foundTypeFromScannedPath, "Was expected to scan a custom path, but 'nested.dll' was not scanned.");
        }
    }
}
