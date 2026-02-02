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

        try
        {
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

            AssertAllAssembliesFromScanPathAreLoadedIntoContext(result, scanPath, customAssemblyLoadContext);
        }
        finally
        {
            customAssemblyLoadContext.Unload();
        }
    }

    [Test]
    public void Should_not_split_load_contexts_when_appdomain_and_filesystem_scanning_are_enabled()
    {
        var scanPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "Messages");

        var testAssemblyLoadContext = AssemblyLoadContext.GetLoadContext(GetType().Assembly) ?? AssemblyLoadContext.Default;

        AssemblyScannerResults result;
        using (testAssemblyLoadContext.EnterContextualReflection())
        {
            var scanner = new AssemblyScanner(scanPath)
            {
                ScanAppDomainAssemblies = true,
                ScanFileSystemAssemblies = true
            };

            result = scanner.GetScannableAssemblies();
        }

        AssertAllAssembliesFromScanPathAreLoadedIntoContext(result, scanPath, testAssemblyLoadContext);
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

        AssertAllAssembliesFromScanPathAreLoadedIntoContext(result, scanPath, testAssemblyLoadContext);
    }

    static void AssertAllAssembliesFromScanPathAreLoadedIntoContext(
        AssemblyScannerResults result,
        string scanPath,
        AssemblyLoadContext expectedLoadContext)
    {
        var loadedFromScanPath = result.Assemblies
            .Where(a =>
                !string.IsNullOrWhiteSpace(a.Location) &&
                a.Location.StartsWith(scanPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.That(loadedFromScanPath, Is.Not.Empty,
            "Expected at least one assembly to be loaded from the scan directory.");

        var mismatches = loadedFromScanPath
            .Select(a => new { Assembly = a, LoadContext = AssemblyLoadContext.GetLoadContext(a) })
            .Where(x => !ReferenceEquals(x.LoadContext, expectedLoadContext))
            .ToList();

        Assert.That(mismatches, Is.Empty,
            "One or more assemblies loaded from the scan path were not loaded into the expected AssemblyLoadContext.");
    }
}