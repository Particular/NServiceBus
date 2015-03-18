namespace NServiceBus.Core.Tests.Config
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NServiceBus.Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_finding_assemblies_to_scan
    {
        List<Assembly> foundAssemblies;

        [SetUp]
        public void SetUp()
        {
            foundAssemblies = GetAssembliesInDirectory(AppDomain.CurrentDomain.BaseDirectory)
                .ToList();
        }

        [Test, Ignore]
        public void Should_for_our_code_exclude_everything_but_NServiceBus_by_default()
        {
            CollectionAssert.AreEquivalent(new String[0],
                foundAssemblies.Where(a => !a.FullName.StartsWith("NServiceBus") && !a.FullName.StartsWith("Obsolete")
                    // FSharp is used as an example external assembly in other tests that is not excluded by default
                    && !a.FullName.StartsWith("TestAssembly")).ToArray());
        }

        [Test]
        public void Should_exclude_system_assemblies()
        {
            CollectionAssert.AreEquivalent(new String[0],
                foundAssemblies.Where(a => a.FullName.StartsWith("System")).ToArray());
        }

        [Test]
        public void Should_exclude_nhibernate_assemblies()
        {
            CollectionAssert.AreEquivalent(new String[0],
                foundAssemblies.Where(a => a.FullName.ToLower().StartsWith("nhibernate")).ToArray());
        }

        IEnumerable<Assembly> GetAssembliesInDirectory(string path, params string[] assembliesToSkip)
        {
            var assemblyScanner = new AssemblyScanner(path);
            if (assembliesToSkip != null)
            {
                assemblyScanner.AssembliesToSkip = assembliesToSkip.ToList();
            }
            return assemblyScanner
                .GetScannableAssemblies()
                .Assemblies;
        }
    }
}
