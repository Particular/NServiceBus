namespace NServiceBus.Core.Analyzer.Tests.Contributors;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Analyzer.Handlers;
using Analyzer.Sagas;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NServiceBus.Core.Analyzer.Tests.Sagas;
using NUnit.Framework;

static class GeneratedCompilation
{
    public static readonly CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Preview)
        .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "NServiceBus")]);

    public static readonly IReadOnlyDictionary<string, string> OptOutOfComposition = new Dictionary<string, string>
    {
        ["build_property.NServiceBusHandlerRegistryComposition"] = "false"
    };

    public static GeneratedOutput Run(string assemblyName, string source, IIncrementalGenerator[] generators, params byte[][] referencedImages) =>
        Run(assemblyName, source, generators, globalOptions: null, referencedImages);

    public static GeneratedOutput Run(string assemblyName, string source, IIncrementalGenerator[] generators, IReadOnlyDictionary<string, string> globalOptions, params byte[][] referencedImages)
    {
        var references = SagaAccessorCompilation.ReferenceAssemblyPaths()
            .Concat(referencedImages.Select(image => MetadataReference.CreateFromImage(image)))
            .ToArray();

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source, ParseOptions, "test.cs")],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(
            generators.Select(generator => generator.AsSourceGenerator()),
            parseOptions: ParseOptions,
            optionsProvider: globalOptions is null ? null : new GlobalOptionsProvider(globalOptions));
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

        return new GeneratedOutput(outputCompilation, generatorDiagnostics, driver.GetRunResult());
    }

    public static byte[] BuildLibrary(string assemblyName, string source, IReadOnlyDictionary<string, string> globalOptions = null)
    {
        var output = Run(assemblyName, source, [new AddHandlerGenerator(), new AddSagaGenerator(), new AddHandlerAndSagasRegistrationGenerator()], globalOptions);
        Assert.That(output.CompilationErrors, Is.Empty);
        return EmitImage(output.Compilation);
    }

    public static byte[] EmitImage(Compilation compilation)
    {
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));
        return stream.ToArray();
    }

    public static Assembly Load(Compilation compilation, params byte[][] referencedImages)
    {
        var image = EmitImage(compilation);

        // One context for all of them so the compiled assemblies resolve their references by name
        var loadContext = new AssemblyLoadContext(null);
        foreach (var referencedImage in referencedImages)
        {
            loadContext.LoadFromStream(new MemoryStream(referencedImage));
        }

        return loadContext.LoadFromStream(new MemoryStream(image));
    }
}

sealed class GlobalOptionsProvider(IReadOnlyDictionary<string, string> globalOptions) : AnalyzerConfigOptionsProvider
{
    public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(globalOptions);

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Empty;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Empty;

    static readonly AnalyzerConfigOptions Empty = new Options(new Dictionary<string, string>());

    sealed class Options(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string value) => values.TryGetValue(key, out value);
    }
}

sealed record GeneratedOutput(Compilation Compilation, ImmutableArray<Diagnostic> GeneratorDiagnostics, GeneratorDriverRunResult RunResult)
{
    public ImmutableArray<GeneratedSourceResult> Sources => [.. RunResult.Results.SelectMany(result => result.GeneratedSources)];

    public ImmutableArray<Diagnostic> CompilationErrors => [.. Compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)];

    public string SourceOf(string hintName) => Sources.Single(s => s.HintName == hintName).SourceText.ToString();
}
