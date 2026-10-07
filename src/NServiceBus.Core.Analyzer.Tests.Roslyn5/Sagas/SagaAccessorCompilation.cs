namespace NServiceBus.Core.Analyzer.Tests.Sagas;

using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Analyzer;
using Analyzer.Sagas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Sagas;

static class SagaAccessorCompilation
{
    public const string AssemblyName = "CollidingAccessors";

    public static Compilation CreateCompilation(string source, CSharpParseOptions parseOptions, bool warningsAsErrors = false, MetadataReference[] references = null) =>
        CSharpCompilation.Create(
            AssemblyName,
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            references ?? ReferenceAssemblyPaths(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable,
                generalDiagnosticOption: warningsAsErrors ? ReportDiagnostic.Error : ReportDiagnostic.Default));

    public static Compilation RunGenerators(Compilation compilation, CSharpParseOptions parseOptions)
    {
        var driver = CSharpGeneratorDriver.Create(
            [
                new AddSagaGenerator().AsSourceGenerator(),
                new AddHandlerAndSagasRegistrationGenerator().AsSourceGenerator()
            ],
            parseOptions: parseOptions);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        return outputCompilation;
    }

    // The registration code lists every interface of a message, which cannot compile for an inaccessible one (Particular/NServiceBus#7968); only the accessors are under test.
    public static Compilation WithoutMessageHierarchies(Compilation compilation)
    {
        var registrationTree = compilation.SyntaxTrees.Single(t => t.ToString().Contains("RegisterMessageTypeWithHierarchy"));
        var withoutHierarchies = Regex.Replace(registrationTree.ToString(), @"(RegisterMessageTypeWithHierarchy\(typeof\([^)]*\)), \[[^\]]*\]\)", "$1, [])");
        return compilation.ReplaceSyntaxTree(registrationTree, CSharpSyntaxTree.ParseText(withoutHierarchies, (CSharpParseOptions)registrationTree.Options, registrationTree.FilePath));
    }

    public static MetadataReference[] ReferenceAssemblyPaths() =>
    [
        .. AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !string.IsNullOrWhiteSpace(a.Location))
            .Select(MetadataReference (a) => MetadataReference.CreateFromFile(a.Location))
    ];

    public static SagaMetadataCollection RegisteredSagas(Assembly assembly)
    {
        var configuration = new EndpointConfiguration("GeneratedAccessors");
        var test = assembly.GetType("Test")!;
        test.GetMethod("Configure")!.Invoke(Activator.CreateInstance(test), [configuration]);
        return configuration.GetSettings().Get<SagaMetadataCollection>();
    }

    // The property finder holds the accessor the saga was registered with, generated or compiled from the mapping expression.
    public static MessagePropertyAccessor MessageAccessor(SagaFinderDefinition finderDefinition)
    {
        var finder = typeof(SagaFinderDefinition).GetProperty("SagaFinder", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(finderDefinition)!;
        return finder.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(f => f.GetValue(finder)).OfType<MessagePropertyAccessor>().Single();
    }
}
