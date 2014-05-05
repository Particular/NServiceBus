namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_inclusion_predicate_is_used
    {
        AssemblyScannerResults results;

        [SetUp]
        public void Context()
        {
            var assemblyScanner = new AssemblyScanner(Path.GetDirectoryName(AssemblyLocation.CurrentDirectory))
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
        public void only_files_explicitly_included_are_returned()
        {
            Assert.That(results.Assemblies, Has.Count.EqualTo(1));
            Assert.That(results.Assemblies.Single().GetName().Name, Is.EqualTo("NServiceBus.Core.Tests"));
        }
    }
}