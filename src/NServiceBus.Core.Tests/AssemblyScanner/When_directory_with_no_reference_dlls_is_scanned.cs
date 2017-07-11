namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.IO;
    using System.Reflection;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_directory_with_no_reference_dlls_is_scanned
    {
        [Test]
        public void assemblies_without_nsb_reference_are_skipped()
        {
            var assemblyToScan = Assembly.LoadFrom(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "dotNet.dll"));
            var scanner = new AssemblyScanner(assemblyToScan);

            var result = scanner.GetScannableAssemblies();

            Assert.That(result.Assemblies.Contains(assemblyToScan), Is.False);
        }

    }
}