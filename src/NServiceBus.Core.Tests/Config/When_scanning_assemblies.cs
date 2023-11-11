namespace NServiceBus.Core.Tests.Config;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hosting.Helpers;
using NUnit.Framework;

[TestFixture]
public class When_finding_assemblies_to_scan
{
    List<Assembly> foundAssemblies;

    [SetUp]
    public void SetUp()
    {
        foundAssemblies = GetAssembliesInDirectory(TestContext.CurrentContext.TestDirectory)
            .ToList();
    }

    [Test]
    [Ignore("Does not work")]
    public void Should_for_our_code_exclude_everything_but_NServiceBus_by_default()
    {
        CollectionAssert.AreEquivalent(System.Array.Empty<string>(),
            foundAssemblies.Where(a => !a.FullName.StartsWith("NServiceBus") && !a.FullName.StartsWith("Obsolete")));
    }

    [Test]
    public void Should_exclude_system_assemblies()
    {
        CollectionAssert.AreEquivalent(System.Array.Empty<string>(),
            foundAssemblies.Where(a => a.FullName.StartsWith("System")).ToArray());
    }

    [Test]
    public void Should_exclude_nhibernate_assemblies()
    {
        CollectionAssert.AreEquivalent(System.Array.Empty<string>(),
            foundAssemblies.Where(a => a.FullName.StartsWith("nhibernate", System.StringComparison.OrdinalIgnoreCase)).ToArray());
    }

    static List<Assembly> GetAssembliesInDirectory(string path, params string[] assembliesToSkip)
    {
        var assemblyScanner = new AssemblyScanner(path)
        {
            ScanAppDomainAssemblies = false
        };

        if (assembliesToSkip != null)
        {
            assemblyScanner.AssembliesToSkip = assembliesToSkip;
        }
        return assemblyScanner
            .GetScannableAssemblies()
            .Assemblies;
    }
}
