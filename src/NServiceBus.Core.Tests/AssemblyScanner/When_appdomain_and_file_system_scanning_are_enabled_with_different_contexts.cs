namespace NServiceBus.Core.Tests.AssemblyScanner;

using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Hosting.Helpers;
using NUnit.Framework;

[TestFixture]
public class When_appdomain_and_file_system_scanning_are_enabled_with_different_contexts
{
    [Test]
    public void Should_not_load_a_second_copy_when_already_loaded_in_a_different_context()
    {
        var scanPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "Messages");
        var messagesAssemblyPath = Path.Combine(scanPath, "Messages.Referencing.Core.dll");

        var appDomainAssemblyLoadContext = new AssemblyLoadContext("ScannerTest_AppDomain_ALC", isCollectible: true);
        var fileSystemAssemblyLoadContext = new AssemblyLoadContext("ScannerTest_FileSystem_ALC", isCollectible: true);

        try
        {
            var preloaded = appDomainAssemblyLoadContext.LoadFromAssemblyPath(messagesAssemblyPath);
            Assert.That(AssemblyLoadContext.GetLoadContext(preloaded), Is.SameAs(appDomainAssemblyLoadContext));

            using (fileSystemAssemblyLoadContext.EnterContextualReflection())
            {
                var scanner = new AssemblyScanner(scanPath)
                {
                    ScanAppDomainAssemblies = true,
                    ScanFileSystemAssemblies = true
                };

                var result = scanner.GetScannableAssemblies();

                var loadedFromScanPath = result.Assemblies
                    .Where(a =>
                        !string.IsNullOrWhiteSpace(a.Location) &&
                        a.Location.StartsWith(scanPath, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                Assert.That(loadedFromScanPath, Is.Not.Empty, "Expected at least one assembly to be loaded from the scan directory.");

                var messagesReferencingCore = loadedFromScanPath
                    .Where(a => a.GetName().Name == "Messages.Referencing.Core")
                    .ToList();

                Assert.That(messagesReferencingCore, Is.Not.Empty, "Expected Messages.Referencing.Core to be part of the scan results.");

                var byFullName = messagesReferencingCore.GroupBy(a => a.FullName).Single();
                Assert.That(byFullName.Count(), Is.EqualTo(1),
                    "Messages.Referencing.Core was loaded more than once (likely into different AssemblyLoadContexts) " +
                    "when both AppDomain and file system scanning are enabled.");

                var single = byFullName.Single();
                var loadContext = AssemblyLoadContext.GetLoadContext(single);

                Assert.That(loadContext, Is.SameAs(appDomainAssemblyLoadContext),
                    "Expected the scanner to reuse the already-loaded AppDomain assembly rather than loading a second copy " +
                    "from disk into the current contextual reflection context.");
            }
        }
        finally
        {
            appDomainAssemblyLoadContext.Unload();
            fileSystemAssemblyLoadContext.Unload();
        }
    }
}