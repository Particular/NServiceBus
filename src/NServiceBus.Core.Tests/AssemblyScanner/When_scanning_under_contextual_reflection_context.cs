namespace NServiceBus.Core.Tests.AssemblyScanner;

using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Hosting.Helpers;
using NUnit.Framework;

[TestFixture]
public class When_scanning_under_contextual_reflection_context
{
    // Verifies that filesystem assemblies loaded by AssemblyScanner are loaded into the current contextual reflection
    // AssemblyLoadContext (when one is set). This is important for plugin/test-runner scenarios to avoid duplicate loads
    // into Default ALC and the resulting type identity issues (e.g. module initializers running twice).
    [Test]
    public void Should_load_filesystem_assemblies_into_the_current_contextual_reflection_alc()
    {
        var scanPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "Messages");
        var customAssemblyLoadContext = new AssemblyLoadContext("ScannerTestALC", isCollectible: true);

        AssemblyScannerResults result;
        // This hides the default NUnit one.
        using (customAssemblyLoadContext.EnterContextualReflection())
        {
            var scanner = new AssemblyScanner(scanPath)
            {
                ScanAppDomainAssemblies = false,
                ScanFileSystemAssemblies = true
            };

            result = scanner.GetScannableAssemblies();
        }

        var loadedFromScanPath = result.Assemblies
            .Where(a =>
                !string.IsNullOrWhiteSpace(a.Location) &&
                a.Location.StartsWith(scanPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.That(loadedFromScanPath, Is.Not.Empty, "Expected at least one assembly to be loaded from the scan directory.");

        var mismatches = loadedFromScanPath
            .Select(a => new
            {
                Assembly = a,
                LoadContext = AssemblyLoadContext.GetLoadContext(a)
            })
            .Where(x => !ReferenceEquals(x.LoadContext, customAssemblyLoadContext))
            .ToList();

        Assert.That(mismatches, Is.Empty,
            "One or more assemblies loaded from the scan path were not loaded into the current contextual reflection ALC.");
    }

    [Test]
    public void Should_load_filesystem_assemblies_into_the_test_assemblyloadcontext()
    {
        var scanPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "Messages");

        var testAssemblyLoadContext = AssemblyLoadContext.GetLoadContext(GetType().Assembly) ?? AssemblyLoadContext.Default;

        var scanner = new AssemblyScanner(scanPath)
        {
            ScanAppDomainAssemblies = false,
            ScanFileSystemAssemblies = true
        };

        var result = scanner.GetScannableAssemblies();

        var loadedFromScanPath = result.Assemblies
            .Where(a =>
                !string.IsNullOrWhiteSpace(a.Location) &&
                a.Location.StartsWith(scanPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.That(loadedFromScanPath, Is.Not.Empty,
            "Expected at least one assembly to be loaded from the scan directory.");

        var mismatches = loadedFromScanPath
            .Select(a => new
            {
                Assembly = a,
                LoadContext = AssemblyLoadContext.GetLoadContext(a)
            })
            .Where(x => !ReferenceEquals(x.LoadContext, testAssemblyLoadContext))
            .ToList();

        Assert.That(mismatches, Is.Empty,
            "One or more assemblies loaded from the scan path were not loaded into the test assembly's AssemblyLoadContext.");
    }
}