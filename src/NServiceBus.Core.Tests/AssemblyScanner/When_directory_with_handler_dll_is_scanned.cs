namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.Linq;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_directory_with_handler_dll_is_scanned
    {
        AssemblyScannerResults results;

        [SetUp]
        public void Context()
        {
            var assemblyScanner = new AssemblyScanner(AssemblyLocation.CurrentDirectory)
                {
                    IncludeAppDomainAssemblies = false
                };

            results = assemblyScanner
                .GetScannableAssemblies();
        }

        [Test]
        public void dll_with_message_handlers_gets_loaded()
        {
            var containsHandlers = "NServiceBus.Core.Tests"; //< assembly name, not file name
            var assembly = results.Assemblies
                .FirstOrDefault(a => a.GetName().Name.Contains(containsHandlers));

            if (assembly == null)
            {
                throw new AssertionException(string.Format("Could not find loaded assembly matching {0}", containsHandlers));
            }
        }
    }
}