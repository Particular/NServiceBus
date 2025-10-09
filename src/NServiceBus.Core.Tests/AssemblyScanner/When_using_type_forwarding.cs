namespace NServiceBus.Core.Tests.AssemblyScanner;

using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using NServiceBus.Hosting.Helpers;
using NUnit.Framework;

[TestFixture]
public class When_using_type_forwarding
{
    // This test is not perfect since it relies on existing binaries to covered assembly scanning scenarios. Since we
    // already use those though the idea of this test is to make sure that the assembly scanner is able to scan all
    // assemblies that have a type forwarding rule within the core assembly. This might turn out to be a broad assumption
    // in the future, and we might have to explicitly remove some but in the meantime this test would have covered us
    // when we moved ICommand, IEvent and IMessages to the message interfaces assembly.
    [Test]
    public void Should_scan_assemblies_indicated_by_the_forwarding_metadata()
    {
        using var fs = File.OpenRead(typeof(AssemblyScanner).Assembly.Location);
        using var peReader = new PEReader(fs);
        var metadataReader = peReader.GetMetadataReader();

        // Exported types only contains a small subset of types, so it's safe to enumerate all of them
        var assemblyNamesOfForwardedTypes = metadataReader.ExportedTypes
            .Select(exportedTypeHandle => metadataReader.GetExportedType(exportedTypeHandle))
            .Where(exportedType => exportedType.IsForwarder)
            .Select(exportedType => (AssemblyReferenceHandle)exportedType.Implementation)
            .Select(assemblyReferenceHandle => metadataReader.GetAssemblyReference(assemblyReferenceHandle))
            .Select(assemblyReference => metadataReader.GetString(assemblyReference.Name))
            .Where(assemblyName => assemblyName.StartsWith("NServiceBus") || assemblyName.StartsWith("Particular"))
            .Distinct()
            .ToList();

        var scanner = new AssemblyScanner(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls"))
        {
            ScanAppDomainAssemblies = false
        };

        var result = scanner.GetScannableAssemblies();
        var assemblyFullNames = result.Assemblies.Select(a => a.GetName().Name).ToList();

        Assert.That(assemblyNamesOfForwardedTypes, Is.SubsetOf(assemblyFullNames));
    }
}