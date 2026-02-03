namespace NServiceBus.Core.Tests.AssemblyScanner;

using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Hosting.Helpers;
using NUnit.Framework;

[TestFixture]
public class When_contextual_reflection_context_is_set
{
    [Test]
    public void Should_load_assemblies_into_the_current_contextual_reflection_context()
    {
        var scanPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "Messages");

        var customAssemblyLoadContext = new AssemblyLoadContext("ScannerTest_CCRC_ALC", isCollectible: true);

        try
        {
            using (customAssemblyLoadContext.EnterContextualReflection())
            {
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

                var byFullName = loadedFromScanPath.GroupBy(a => a.FullName);
                foreach (var group in byFullName)
                {
                    Assert.That(group.Count(), Is.EqualTo(1),
                        $"Assembly {group.Key} was loaded from more than one AssemblyLoadContext.");
                }

                var messagesAssembly = loadedFromScanPath.Single(a => a.GetName().Name == "Messages.Referencing.Core");

                var loadContext = AssemblyLoadContext.GetLoadContext(messagesAssembly);

                Assert.That(loadContext, Is.SameAs(customAssemblyLoadContext),
                    "The scanner did not use CurrentContextualReflectionContext when loading assemblies from disk.");
            }
        }
        finally
        {
            customAssemblyLoadContext.Unload();
        }
    }
}