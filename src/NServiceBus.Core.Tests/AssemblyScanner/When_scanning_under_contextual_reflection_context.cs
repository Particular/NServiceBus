namespace NServiceBus.Core.Tests.AssemblyScanner;

using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Hosting.Helpers;
using NUnit.Framework;

[TestFixture]
[NonParallelizable]
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
        scanPath = CopyToUniqueScanDirectory(scanPath);

        var testAssemblyLoadContext = AssemblyLoadContext.GetLoadContext(GetType().Assembly) ?? AssemblyLoadContext.Default;

        try
        {
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

            // With ScanAppDomainAssemblies=true, assemblies might already be loaded and come from outside scanPath.
            // Assert by assembly identity rather than Location.
            AssertAssemblyIsLoadedIntoContext(result, "Messages.Referencing.Core", testAssemblyLoadContext);
            AssertAssemblyIsLoadedIntoContext(result, "Messages.Referencing.MessageInterfaces", testAssemblyLoadContext);
        }
        finally
        {
            Directory.Delete(scanPath, true);
        }
    }

    static string CopyToUniqueScanDirectory(string sourceDir)
    {
        var targetDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "AssemblyScanner", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*.*", SearchOption.TopDirectoryOnly)
                     .Where(f =>
                         f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                         f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)))
        {
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));
        }

        return targetDir;
    }

    static void AssertAssemblyIsLoadedIntoContext(
        AssemblyScannerResults result,
        string simpleName,
        AssemblyLoadContext expectedLoadContext)
    {
        var assembly = result.Assemblies.FirstOrDefault(a => string.Equals(a.GetName().Name, simpleName, StringComparison.Ordinal));
        Assert.That(assembly, Is.Not.Null, $"Expected '{simpleName}' to be present in scanned assemblies.");

        var actualLoadContext = AssemblyLoadContext.GetLoadContext(assembly!);
        Assert.That(ReferenceEquals(actualLoadContext, expectedLoadContext), Is.True,
            $"Expected '{simpleName}' to be loaded into AssemblyLoadContext '{expectedLoadContext.Name}', but was '{actualLoadContext?.Name}'.");
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