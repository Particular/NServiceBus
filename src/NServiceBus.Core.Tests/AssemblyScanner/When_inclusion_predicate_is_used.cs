namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.Collections.Generic;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_inclusion_predicate_is_used
    {
        AssemblyScannerResults results;

        [SetUp]
        public void Context()
        {
            var assemblyScanner = new AssemblyScanner(AssemblyLocation.CurrentDirectory)
                {
                    AssembliesToInclude = new List<string>
                        {
                            "NServiceBus.Core.Tests.dll"
                        }
                };
            results = assemblyScanner
                .GetScannableAssemblies();
        }

        [Test]
        public void includes_explicitly_included_file()
        {
            Assert.IsTrue(results.Assemblies.Exists(a => a.GetName().Name == "NServiceBus.Core.Tests"));
        }
    }
}