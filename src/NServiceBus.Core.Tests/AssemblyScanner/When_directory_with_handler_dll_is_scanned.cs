namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.Linq;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_directory_with_handler_dll_is_scanned
    {
        [Test]
        public void Dll_with_message_handlers_gets_loaded()
        {
            var assemblyScanner = new AssemblyScanner(TestContext.CurrentContext.TestDirectory)
            {
                ScanAppDomainAssemblies = false
            };

            var results = assemblyScanner
                .GetScannableAssemblies();

            var containsHandlers = "NServiceBus.Core.Tests"; //< assembly name, not file name
            var assembly = results.Assemblies
                .FirstOrDefault(a => a.GetName().Name.Contains(containsHandlers));

            if (assembly == null)
            {
                throw new AssertionException($"Could not find loaded assembly matching {containsHandlers}");
            }
        }
    }
}