namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.IO;
    using System.Linq;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_scanning_assemblies_with_circular_dependencies
    {
        [Test]
        public void ReferencesNServiceBus_circular()
        {
            var scanner = new AssemblyScanner(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "circular"));
            scanner.ScanAppDomainAssemblies = false;

            var result = scanner.GetScannableAssemblies();

            Assert.That(result.Assemblies.Any(a => a.FullName.Contains("ClassLibraryA")), Is.False);
            Assert.That(result.Assemblies.Any(a => a.FullName.Contains("ClassLibraryB")), Is.False);
        }
    }
}