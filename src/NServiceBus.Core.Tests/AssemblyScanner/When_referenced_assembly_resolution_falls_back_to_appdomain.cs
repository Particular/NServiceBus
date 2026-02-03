#nullable enable
namespace NServiceBus.Core.Tests.AssemblyScanner;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Hosting.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

[TestFixture]
public class When_referenced_assembly_resolution_falls_back_to_appdomain
{
    [Test]
    public void Should_prefer_exact_identity_match_over_simple_name_when_multiple_are_loaded()
    {
        var tempDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var fakeMessageInterfacesPath = Path.Combine(tempDir, "NServiceBus.MessageInterfaces.dll");
        var messagesAssemblyPath = Path.Combine(tempDir, "Messages.Referencing.MessageInterfaces.dll");

        var fakeVersion = new Version(99, 0, 0, 0);

        // Ensure the real NServiceBus.MessageInterfaces is already loaded in the test process.
        var realMessageInterfaces = typeof(IMessage).Assembly;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(realMessageInterfaces.GetName().Name, Is.EqualTo("NServiceBus.MessageInterfaces"));
            Assert.That(realMessageInterfaces.GetName().Version, Is.Not.EqualTo(fakeVersion));
        }

        // We'll reference NServiceBus.Core to make the generated assembly scannable.
        // (Avoid ICommand because it lives in NServiceBus.MessageInterfaces and causes duplicate IMessage definitions.)
        var coreAssemblyPath = typeof(EndpointConfiguration).Assembly.Location;
        Assert.That(File.Exists(coreAssemblyPath), Is.True,
            "Test setup error: could not locate NServiceBus.Core assembly on disk.");

        // 1) Compile a fake, unsigned NServiceBus.MessageInterfaces v99 defining NServiceBus.IMessage.
        CompileCSharpAssembly(
            assemblyName: "NServiceBus.MessageInterfaces",
            assemblyVersion: fakeVersion,
            outputPath: fakeMessageInterfacesPath,
            sourceCode: """
                        namespace NServiceBus
                        {
                            public interface IMessage { }
                        }
                        """,
            references: GetBasicRuntimeReferences());

        // 2) Compile an assembly that:
        //    - references the fake v99 IMessage
        //    - references NServiceBus.Core (so AssemblyScanner considers it scannable via core reference)
        //    - does NOT reference the real NServiceBus.MessageInterfaces
        CompileCSharpAssembly(
            assemblyName: "Messages.Referencing.MessageInterfaces",
            assemblyVersion: new Version(1, 0, 0, 0),
            outputPath: messagesAssemblyPath,
            sourceCode: """
                        namespace MyMessages
                        {
                            using NServiceBus;
                        
                            public class MyMessage : IMessage { }
                            
                            // Force a reference to NServiceBus.Core without pulling in the real MessageInterfaces assembly.
                            public static class TouchCore
                            {
                                public static System.Type CoreType => typeof(EndpointConfiguration);
                            }
                        }
                        """,
            references: GetBasicRuntimeReferences().Concat([
                MetadataReference.CreateFromFile(fakeMessageInterfacesPath),
                MetadataReference.CreateFromFile(coreAssemblyPath)
            ]));

        var v99Alc = new AssemblyLoadContext("ScannerTest_FakeMessageInterfacesV99", isCollectible: true);

        // 4) Load "messages" into an ALC that does NOT resolve by name, to force the scanner into the fallback path.
        var scanningAlc = new ThrowingAssemblyLoadContext(name: "ScannerTest_Scanning_ThrowOnMI", throwOnSimpleName: "NServiceBus.MessageInterfaces", isCollectible: true);

        try
        {
            var fakeMi = v99Alc.LoadFromAssemblyPath(fakeMessageInterfacesPath);
            Assert.That(fakeMi.GetName().Name, Is.EqualTo("NServiceBus.MessageInterfaces"));
            Assert.That(fakeMi.GetName().Version, Is.EqualTo(fakeVersion));

            _ = scanningAlc.LoadFromAssemblyPath(messagesAssemblyPath);

            var scanner = new AssemblyScanner
            {
                ScanAppDomainAssemblies = true,
                ScanFileSystemAssemblies = false,
                ThrowExceptions = false
            };

            var results = scanner.GetScannableAssemblies();

            var resolvedCandidates = results.Assemblies
                .Where(a => a.GetName().Name == "NServiceBus.MessageInterfaces")
                .ToList();

            Assert.That(resolvedCandidates, Is.Not.Empty,
                "Expected NServiceBus.MessageInterfaces to appear in scan results as a referenced dependency.");

            var resolvedV99 = resolvedCandidates.SingleOrDefault(a => a.GetName().Version == fakeVersion);
            Assert.That(resolvedV99, Is.Not.Null,
                "Expected the scanner to resolve the exact referenced identity: NServiceBus.MessageInterfaces, Version=99.0.0.0.");

            Assert.That(AssemblyLoadContext.GetLoadContext(resolvedV99!), Is.SameAs(v99Alc),
                "Expected the resolved v99 assembly to come from the ALC where it was loaded.");
        }
        finally
        {
            scanningAlc.Unload();
            v99Alc.Unload();
        }
    }

    static IEnumerable<MetadataReference> GetBasicRuntimeReferences() =>
        new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Assembly).Assembly.Location),
            }
            .Cast<PortableExecutableReference>()
            .GroupBy(r => r.FilePath, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First());

    static void CompileCSharpAssembly(
        string assemblyName,
        Version assemblyVersion,
        string outputPath,
        string sourceCode,
        IEnumerable<MetadataReference> references)
    {
        var versionAttributes = $"""
            using System.Reflection;

            [assembly: AssemblyVersion("{assemblyVersion}")]
            [assembly: AssemblyFileVersion("{assemblyVersion}")]
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(versionAttributes + "\n" + sourceCode);

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));

        var emitResult = compilation.Emit(outputPath);

        if (!emitResult.Success)
        {
            var diagnostics = string.Join(Environment.NewLine, emitResult.Diagnostics.Select(d => d.ToString()));
            Assert.Fail($"Failed to compile test assembly '{assemblyName}'. Diagnostics:{Environment.NewLine}{diagnostics}");
        }

        Assert.That(File.Exists(outputPath), Is.True, $"Expected compiled assembly at '{outputPath}'.");
    }

    sealed class ThrowingAssemblyLoadContext(string name, string throwOnSimpleName, bool isCollectible) : AssemblyLoadContext(name, isCollectible)
    {
        protected override Assembly? Load(AssemblyName assemblyName) =>
            string.Equals(assemblyName.Name, throwOnSimpleName, StringComparison.Ordinal) ? throw
                new FileNotFoundException($"Deliberately failing resolution for '{assemblyName.FullName}'.") : null;
    }
}