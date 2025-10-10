#nullable enable
namespace NServiceBus.Core.Analyzer.Tests.Helpers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using Particular.Approvals;

public class SourceGeneratorTest
{
    readonly List<(string Filename, string Source)> sources;
    readonly List<ISourceGenerator> generators;
    readonly Dictionary<string, string> features;
    readonly string outputAssemblyName;
    string? scenarioName;
    Compilation? initialCompilation;
    Compilation? outputCompilation;
    ImmutableArray<Diagnostic> generatorDiagnostics;
    ImmutableArray<Diagnostic> compilationDiagnostics;
    bool suppressDiagnosticErrors;
    bool suppressCompilationErrors;
    bool wroteToConsole;

    SourceGeneratorTest(string? outputAssemblyName = null)
    {
        sources = [];
        generators = [];
        this.outputAssemblyName = outputAssemblyName ?? "TestAssembly";

        features = new()
        {
            ["InterceptorsNamespaces"] = "NServiceBus"
        };

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            {
                References.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        References.Add(MetadataReference.CreateFromFile(typeof(IMessage).Assembly.Location));
        References.Add(MetadataReference.CreateFromFile(typeof(EndpointConfiguration).Assembly.Location));
    }

    public static SourceGeneratorTest ForSourceGenerator<TGenerator>([CallerMemberName] string? outputAssemblyName = null)
        where TGenerator : ISourceGenerator, new()
        => new SourceGeneratorTest(outputAssemblyName).WithSourceGenerator<TGenerator>();

    public static SourceGeneratorTest ForIncrementalGenerator<TGenerator>([CallerMemberName] string? outputAssemblyName = null)
        where TGenerator : IIncrementalGenerator, new()
        => new SourceGeneratorTest(outputAssemblyName).WithIncrementalGenerator<TGenerator>();

    public List<MetadataReference> References { get; } = [];
    public LanguageVersion LangVersion { get; set; } = LanguageVersion.LatestMajor;

    public SourceGeneratorTest WithSource(string source, string? filename = null)
    {
        filename ??= $"Source{sources.Count:00}.cs";
        sources.Add((filename, source));
        return this;
    }

    public SourceGeneratorTest WithScenarioName(string name)
    {
        scenarioName = name;
        return this;
    }

    public SourceGeneratorTest AddReference(MetadataReference reference)
    {
        References.Add(reference);
        return this;
    }

    public SourceGeneratorTest SuppressDiagnosticErrors()
    {
        suppressDiagnosticErrors = true;
        return this;
    }

    public SourceGeneratorTest SuppressCompilationErrors()
    {
        suppressCompilationErrors = true;
        return this;
    }

    public SourceGeneratorTest WithIncrementalGenerator<TGenerator>() where TGenerator : IIncrementalGenerator, new()
    {
        generators.Add(new TGenerator().AsSourceGenerator());
        return this;
    }

    public SourceGeneratorTest WithSourceGenerator<TGenerator>() where TGenerator : ISourceGenerator, new()
    {
        generators.Add(new TGenerator());
        return this;
    }

    public SourceGeneratorTest WithProperty(string name, string value)
    {
        features.Add(name, value);
        return this;
    }

    public SourceGeneratorTest Run()
    {
        if (outputCompilation is not null)
        {
            return this;
        }

        if (generators.Count == 0)
        {
            throw new Exception("No generators added");
        }

        var parseOptions = new CSharpParseOptions(LangVersion)
            .WithFeatures(features);

        var syntaxTrees = sources
            .Select(src =>
            {
                var tree = CSharpSyntaxTree.ParseText(src.Source, path: src.Filename);
                var options = parseOptions;
                return tree.WithRootAndOptions(tree.GetRoot(), options);
            });

        var driverOpts = new GeneratorDriverOptions(
            disabledOutputs: IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: true);

        var optsProvider = new OptionsProvider(new DictionaryAnalyzerOptions(features));

        var driver = CSharpGeneratorDriver.Create(generators,
            driverOptions: driverOpts,
            optionsProvider: optsProvider,
            parseOptions: parseOptions);

        var compileOpts = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        initialCompilation = CSharpCompilation.Create(outputAssemblyName, syntaxTrees, References, compileOpts);

        driver.RunGeneratorsAndUpdateCompilation(initialCompilation, out outputCompilation, out generatorDiagnostics);

        try
        {
            if (!suppressDiagnosticErrors)
            {
                Assert.That(generatorDiagnostics, Has.None.Matches<Diagnostic>(d => d.Severity >= DiagnosticSeverity.Error));
            }

            compilationDiagnostics = outputCompilation.GetDiagnostics();

            if (!suppressCompilationErrors)
            {
                Assert.That(compilationDiagnostics, Has.None.Matches<Diagnostic>(d => d.Severity >= DiagnosticSeverity.Warning));
            }

            return this;
        }
        catch (AssertionException)
        {
            _ = ToConsole();
            throw;
        }
    }

    public string GetCompilationOutput(bool withLineNumbers = false)
    {
        if (outputCompilation is null)
        {
            _ = Run();
        }

        var sb = new StringBuilder();

        void WriteHeading(string heading)
        {
            if (sb.Length > 0)
            {
                _ = sb.AppendLine();
            }

            var start = $"// == {heading} ";

            _ = sb.Append(start);

            for (var i = 0; i < (120 - start.Length); i++)
            {
                _ = sb.Append('=');
            }

            _ = sb.AppendLine();
        }

        if (generatorDiagnostics.Any())
        {
            WriteHeading("Generator Diagnostics");
            foreach (var diagnostic in generatorDiagnostics)
            {
                _ = sb.AppendLine(diagnostic.ToString());
            }
        }

        if (compilationDiagnostics.Any())
        {
            WriteHeading("Compilation Diagnostics");
            foreach (var diagnostic in compilationDiagnostics)
            {
                _ = sb.AppendLine(diagnostic.ToString());
            }
        }

        foreach (var syntaxTree in outputCompilation!.SyntaxTrees)
        {
            WriteHeading(syntaxTree.FilePath);

            if (withLineNumbers)
            {
                var lines = syntaxTree.GetText().Lines;
                var padSize = lines.Count.ToString().Length;
                foreach (var line in lines)
                {
                    _ = sb.AppendLine($"{(line.LineNumber + 1).ToString().PadLeft(padSize)}: {line.Text?.GetSubText(line.Span)}");
                }
            }
            else
            {
                sb.AppendLine(syntaxTree.ToString());
            }
        }

        return sb.ToString();
    }

    public SourceGeneratorTest Approve([CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null)
    {
        if (outputCompilation is null)
        {
            _ = Run();
        }

        try
        {
            var output = GetCompilationOutput();
            Approver.Verify(output, scenario: scenarioName, callerFilePath: callerFilePath, callerMemberName: callerMemberName);
            return this;
        }
        catch (Exception)
        {
            _ = ToConsole();
            throw;
        }
    }

    public SourceGeneratorTest ToConsole()
    {
        if (wroteToConsole)
        {
            return this;
        }
        if (outputCompilation is null)
        {
            _ = Run();
        }

        var output = GetCompilationOutput(true);
        Console.WriteLine(output);
        wroteToConsole = true;
        return this;
    }

    class OptionsProvider(AnalyzerConfigOptions options) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => options;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => options;
        public override AnalyzerConfigOptions GlobalOptions => options;
    }

    internal sealed class DictionaryAnalyzerOptions(Dictionary<string, string> properties) : AnalyzerConfigOptions
    {
        public static DictionaryAnalyzerOptions Empty { get; } = new([]);

        public override bool TryGetValue(string key, out string value)
            => properties.TryGetValue(key, out value!);
    }
}