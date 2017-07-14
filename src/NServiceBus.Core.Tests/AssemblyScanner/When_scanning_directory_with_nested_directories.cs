namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.IO;
    using System.Linq;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_scanning_directory_with_nested_directories
    {
        [Test]
        public void Should_not_scan_nested_directories_by_default()
        {
            var scanner = new AssemblyScanner(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "Nested"));
            scanner.ScanAppDomainAssemblies = false;

            var result = scanner.GetScannableAssemblies();

            var foundTypeFromNestedAssembly = result.Types.Any(x => x.Name == "NestedClass");
            var foundTypeFromDerivedAssembly = result.Types.Any(x => x.Name == "DerivedClass");

            Assert.False(foundTypeFromNestedAssembly, "Was expected not to scan nested assemblies, but 'nested.dll' was scanned.");
            Assert.False(foundTypeFromDerivedAssembly, "Was expected not to scan nested assemblies, but 'Derived.dll' was scanned.");
        }

        [Test]
        public void Should_scan_nested_directories_if_requested()
        {
            var scanner = new AssemblyScanner(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "Nested"))
            {
                ScanNestedDirectories = true,
                ScanAppDomainAssemblies = false
            };

            var result = scanner.GetScannableAssemblies();

            var foundTypeFromNestedAssembly = result.Types.Any(x => x.Name == "NestedClass");
            var foundTypeFromDerivedAssembly = result.Types.Any(x => x.Name == "DerivedClass");

            Assert.True(foundTypeFromNestedAssembly, "Was expected to scan nested assemblies, but 'nested.dll' was not scanned.");
            Assert.True(foundTypeFromDerivedAssembly, "Was expected to scan nested assemblies, but 'Derived.dll' was not scanned.");
        }
    }
}