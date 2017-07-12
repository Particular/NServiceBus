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
            // Assemblies already exist in TestDlls/circular:
            var circularDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestDlls\circular");

            var classLibA = Path.Combine(circularDirectory, "ClassLibraryA.dll");
            var classLibB = Path.Combine(circularDirectory, "ClassLibraryB.dll");

            // Put assemblies in TestContext.CurrentContext.TestDirectory so they resolve correctly
            var tempClassLibA = Path.Combine(TestContext.CurrentContext.TestDirectory, "ClassLibraryA.dll");
            var tempClassLibB = Path.Combine(TestContext.CurrentContext.TestDirectory, "ClassLibraryB.dll");

            File.Copy(classLibA, tempClassLibA, true);
            File.Copy(classLibB, tempClassLibB, true);

            var scanner = new AssemblyScanner(Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestDlls\circular"));
            scanner.ScanAppDomainAssemblies = false;

            var result = scanner.GetScannableAssemblies();

            Assert.That(result.Assemblies.Any(a => a.FullName.Contains("ClassLibraryA")), Is.False);
            Assert.That(result.Assemblies.Any(a => a.FullName.Contains("ClassLibraryB")), Is.False);
        }
    }
}