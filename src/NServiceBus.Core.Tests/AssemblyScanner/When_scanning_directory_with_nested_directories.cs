namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.Linq;
    using NServiceBus.Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_scanning_directory_with_nested_directories
    {
        [Test]
        public void Should_not_scan_nested_directories_by_default()
        {
            var assemblyScanner = new AssemblyScanner(AssemblyScannerTests.GetTestAssemblyDirectory());

            var scannedNestedAssembly = assemblyScanner.GetScannableAssemblies().SkippedFiles.Any(x => x.FilePath.EndsWith("nested.dll"));
            Assert.False(scannedNestedAssembly, "Was expected not to scan nested assemblies, but nested assembly was scanned.");
        }


        [Test]
        public void Should_scan_nested_directories_if_requested()
        {
            var assemblyScanner = new AssemblyScanner(AssemblyScannerTests.GetTestAssemblyDirectory())
            {
                ScanNestedDirectories = true
            };

            var scannedNestedAssembly = assemblyScanner.GetScannableAssemblies().SkippedFiles.Any(x => x.FilePath.EndsWith("nested.dll"));
            Assert.True(scannedNestedAssembly, "Was expected to scan nested assemblies, but nested assembly were not scanned.");
        }
    }
}