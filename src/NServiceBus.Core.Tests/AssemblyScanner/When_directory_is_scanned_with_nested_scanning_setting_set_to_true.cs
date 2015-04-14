namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.Configuration;
    using System.Linq;
    using NServiceBus.Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_directory_is_scanned_with_nested_scanning_setting_set_to_true
    {
        AssemblyScannerResults assemblyScannerResults;
        KeyValueConfigurationCollection originalAppSettings = new KeyValueConfigurationCollection();

        [SetUp]
        public void Context()
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var appSettings = (AppSettingsSection)configuration.GetSection("appSettings");
            foreach (var key in appSettings.Settings.AllKeys)
            {
                originalAppSettings.Add(key, appSettings.Settings[key].Value);
            }
            appSettings.Settings.Clear();
            appSettings.Settings.Add("NServiceBus/AssemblyScanning/ScanNestedDirectories", "true");
            configuration.Save();
            ConfigurationManager.RefreshSection("appSettings");

            var assemblyScanner = new AssemblyScanner(AssemblyScannerTests.GetTestAssemblyDirectory())
            {
                IncludeAppDomainAssemblies = false
            };

            assemblyScannerResults = assemblyScanner.GetScannableAssemblies();
        }

        [TearDown]
        public void TearDown()
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var appSettings = (AppSettingsSection)configuration.GetSection("appSettings");
            appSettings.Settings.Clear();
            foreach (var key in originalAppSettings.AllKeys)
            {
                appSettings.Settings.Add(key, originalAppSettings[key].Value);
            }
            configuration.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }

        [Test]
        public void Should_not_scan_nested_folders()
        {
            var scannedNestedAssembly = assemblyScannerResults.SkippedFiles.Any(x => x.FilePath.EndsWith("nested.dll"));
            Assert.True(scannedNestedAssembly, "Was expected to scan nested assemblies, but nested assembly were not scanned.");
        }

    }
}