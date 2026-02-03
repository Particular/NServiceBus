namespace NServiceBus.Core.Tests.AssemblyScanner;

using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Hosting.Helpers;
using NUnit.Framework;

[TestFixture]
public class When_more_than_one_AssemblyLoadContext_has_scannable_types
{
    [Test]
    public void Should_only_load_one_copy_of_the_assembly()
    {
        var scanPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "Messages");
        var customAssemblyLoadContext = new AssemblyLoadContext("ScannerTestALC", isCollectible: true);

        customAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(scanPath, "Messages.Referencing.Core.dll"));

        var scanner = new AssemblyScanner(scanPath);
        var result = scanner.GetScannableAssemblies();

        var loadedFromScanPath = result.Assemblies
            .Where(a =>
                !string.IsNullOrWhiteSpace(a.Location) &&
                a.Location.StartsWith(scanPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.That(loadedFromScanPath, Is.Not.Empty, "Expected at least one assembly to be loaded from the scan directory.");

        var assemblies = loadedFromScanPath.GroupBy(a => a.FullName);

        foreach (var assembly in assemblies)
        {
            var numberOfTimesLoaded = assembly.Count();
            Assert.That(numberOfTimesLoaded, Is.EqualTo(1), $"Assembly {assembly.Key} was loaded from more than one AssemblyLoadContext.");
        }

        var messagesAssembly = loadedFromScanPath.Single(a => a.FullName.StartsWith("Messages.Referencing.Core"));
        var loadContext = AssemblyLoadContext.GetLoadContext(messagesAssembly);

        Assert.That(loadContext.Name, Is.EqualTo("ScannerTestALC"), "The wrong AssemblyLoadContext was used to load the assembly.");
    }
}