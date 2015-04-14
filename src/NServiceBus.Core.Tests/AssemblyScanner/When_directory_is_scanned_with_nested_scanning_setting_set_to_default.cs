namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.Linq;
    using NServiceBus.Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_directory_is_scanned_with_nested_scanning_setting_set_to_default
    {
        AssemblyScannerResults assemblyScannerResults;

        [SetUp]
        public void Context()
        {
            var assemblyScanner = new AssemblyScanner(AssemblyScannerTests.GetTestAssemblyDirectory())
                {
                    IncludeAppDomainAssemblies = false
                };

            assemblyScannerResults = assemblyScanner.GetScannableAssemblies();
        }

        [Test]
        public void Should_not_scan_nested_directories()
        {
            var scannedNestedAssembly = assemblyScannerResults.SkippedFiles.Any(x => x.FilePath.EndsWith("nested.dll"));
            Assert.False(scannedNestedAssembly, "Was expected not to scan nested assemblies, but nested assembly was scanned.");
        }

    }

    ////
}