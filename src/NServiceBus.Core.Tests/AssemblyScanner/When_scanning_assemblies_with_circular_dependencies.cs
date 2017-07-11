namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System;
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
            // Assemblies already exist in TestDlls/circular:
            var circularDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestDlls\circular");
            var classLibB = Path.Combine(circularDirectory, "ClassLibraryB.dll");

            // Put ClassLibraryB.dll in CurrentDomain.BaseDirectory so it resolves correctly
            var tempClassLibB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClassLibraryB.dll");
            File.Copy(classLibB, tempClassLibB, true);

            var scanner = new AssemblyScanner(Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestDlls\circular"));
            scanner.ScanAppDomainAssemblies = false;

            var result = scanner.GetScannableAssemblies();

            Assert.That(result.Assemblies.Any(a => a.FullName.Contains("ClassLibraryA")), Is.False);
            Assert.That(result.Assemblies.Any(a => a.FullName.Contains("ClassLibraryB")), Is.False);
        }
    }
}