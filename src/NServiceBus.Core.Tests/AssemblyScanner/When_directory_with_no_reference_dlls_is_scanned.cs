namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.IO;
    using System.Linq;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_directory_with_no_reference_dlls_is_scanned
    {
        [Test]
        public void assemblies_without_nsb_reference_are_skipped()
        {
            var assemblyScanner = new AssemblyScanner(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls"));
            assemblyScanner.ScanAppDomainAssemblies = false;

            var results = assemblyScanner
                .GetScannableAssemblies();

            Assert.That(results.Assemblies.Any(a => a.FullName.Contains("dotNet.dll")), Is.False);
        }

    }
}