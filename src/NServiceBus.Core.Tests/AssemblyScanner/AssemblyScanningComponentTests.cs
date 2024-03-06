namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.IO;
    using System.Linq;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    class AssemblyScanningComponentTests
    {
        [Test]
        public void Should_initialize_scanner_with_custom_path_when_provided()
        {
            var settingsHolder = new SettingsHolder();
            settingsHolder.Set(new HostingComponent.Settings(settingsHolder));

            var configuration = new AssemblyScanningComponent.Configuration(settingsHolder);

            configuration.AssemblyScannerConfiguration.AdditionalAssemblyScanningPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "Nested", "Subfolder");

            var conventions = new Conventions();

            var component = AssemblyScanningComponent.Initialize(configuration, settingsHolder, conventions);

            var foundTypeFromScannedPath = component.AvailableTypes.Any(x => x.Name == "NestedClass");

            Assert.True(foundTypeFromScannedPath, "Was expected to scan a custom path, but 'nested.dll' was not scanned.");
        }
    }
}
