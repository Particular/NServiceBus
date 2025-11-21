#nullable enable
namespace NServiceBus.Core.Analyzer.Tests.Helpers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using Particular.Approvals;

public partial class SourceGeneratorTest
{
    readonly List<(string Filename, string Source)> sources;
    readonly List<ISourceGenerator> generators;
    readonly Dictionary<string, string> features;
    readonly string outputAssemblyName;
    string? scenarioName;
    Compilation? initialCompilation;
    Build? build;
    Build? clonedBuild;
    ImmutableArray<Diagnostic> compilationDiagnostics;
    bool suppressDiagnosticErrors;
    bool suppressCompilationErrors;
    bool wroteToConsole;
    GeneratorTestOutput outputType;
    readonly HashSet<string> generatorStages = new(StringComparer.OrdinalIgnoreCase);
    readonly List<string> generatorStagesList = [];

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

    public LanguageVersion LangVersion { get; set; } = LanguageVersion.CSharp14;

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

    public SourceGeneratorTest ControlOutput(GeneratorTestOutput output)
    {
        outputType = output;
        return this;
    }

    public SourceGeneratorTest WithGeneratorStages(params string[] stages)
    {
        foreach (var stage in stages)
        {
            if (generatorStages.Add(stage))
            {
                generatorStagesList.Add(stage);
            }
        }
        return this;
    }

    [MemberNotNull(nameof(build))]
    public SourceGeneratorTest Run()
    {
        if (build is not null)
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

        build = new Build(initialCompilation, driver);

        try
        {
            if (!suppressDiagnosticErrors)
            {
                Assert.That(build.GeneratorDiagnostics, Has.None.Matches<Diagnostic>(d => d.Severity >= DiagnosticSeverity.Error));
            }

            compilationDiagnostics = build.OutputCompilation.GetDiagnostics();

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

    [MemberNotNull(nameof(clonedBuild)), MemberNotNull(nameof(build))]
    void RunClonedBuild()
    {
        if (build is null)
        {
            _ = Run();
        }

        clonedBuild ??= build.Clone();
    }

    public SourceGeneratorTest AssertRunsAreEqual()
    {
        if (generatorStages.Count == 0)
        {
            throw new Exception("Must add GeneratorStages first.");
        }

        RunClonedBuild();

        Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> GetTracked(GeneratorDriverRunResult result)
            => result.Results.SelectMany(r => r.TrackedSteps.Where(step => generatorStages.Contains(step.Key)))
                .ToDictionary();

        var trackedSteps1 = GetTracked(build.RunResult);
        var trackedSteps2 = GetTracked(clonedBuild.RunResult);

        Assert.That(trackedSteps1, Is.Not.Empty);
        Assert.That(trackedSteps2.Count, Is.EqualTo(trackedSteps1.Count));
        Assert.That(trackedSteps1.Keys, Is.EquivalentTo(trackedSteps2.Keys));

        using (Assert.EnterMultipleScope())
        {
            foreach (var step in trackedSteps1)
            {
                var runStep1 = step.Value;
                var runStep2 = trackedSteps2[step.Key];
                AssertStepsAreEqual(step.Key, runStep1, runStep2);
            }
        }

        return this;
    }

    static void AssertStepsAreEqual(string trackingName, ImmutableArray<IncrementalGeneratorRunStep> steps1, ImmutableArray<IncrementalGeneratorRunStep> steps2)
    {
        Assert.That(steps1, Has.Length.EqualTo(steps2.Length));

        for (var i = 0; i < steps1.Length; i++)
        {
            var step1 = steps1[i];
            var step2 = steps2[i];

            var out1 = step1.Outputs.Select(o => o.Value).ToArray();
            var out2 = step2.Outputs.Select(o => o.Value).ToArray();

            Assert.That(out1, Is.EqualTo(out2).UsingPropertiesComparer(), $"Step '{trackingName}' outputs are not the same between runs, but should be cacheable results.");

            var outputReasons = step2.Outputs.Select(o => o.Reason).ToArray();
            var badReasons = outputReasons.Where(reason => reason is not IncrementalStepRunReason.Cached and not IncrementalStepRunReason.Unchanged).ToArray();

            Assert.That(badReasons.Length, Is.EqualTo(0), $"Step '{trackingName}' outputs contain reasons: {string.Join(',', badReasons)}. Should all be Cached or Unchanged to be memoizable.");

            // Not doing anything here to explicitly assert that types are not Compilation, ISymbol, SyntaxNode or other
            // types known to be bad ideas, but that would require nasty reflection to traverse an object graph
        }
    }

    public string GetCompilationOutput(bool withLineNumbers = false)
    {
        if (build is null)
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

        if (build.GeneratorDiagnostics.Any())
        {
            WriteHeading("Generator Diagnostics");
            foreach (var diagnostic in build.GeneratorDiagnostics)
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

        foreach (var syntaxTree in FilteredSyntaxTrees())
        {
            WriteHeading(syntaxTree.FilePath.Replace('\\', '/'));

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

        return sb.ToString().TrimEnd();
    }

    public SourceGeneratorTest Approve(Func<string, string>? scrubber = null, [CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null)
    {
        if (build is null)
        {
            _ = Run();
        }

        try
        {
            var output = GetCompilationOutput();
            var toApprove = ScrubPlatformSpecificInterceptorData().Replace(output, m => m.Value.Replace(m.Groups["InterceptData"].Value, "{PLATFORM-SPECIFIC-BASE64-DATA}"));
            Approver.Verify(toApprove, scrubber, scenarioName, callerFilePath, callerMemberName);
            return this;
        }
        catch (Exception)
        {
            _ = ToConsole();
            throw;
        }
    }

    [GeneratedRegex(@"System\.Runtime\.CompilerServices\.InterceptsLocationAttribute\(1, ""(?<InterceptData>[A-Za-z0-9+=/]{36})""\)", RegexOptions.Compiled | RegexOptions.NonBacktracking)]
    private static partial Regex ScrubPlatformSpecificInterceptorData();

    public SourceGeneratorTest ToConsole()
    {
        if (wroteToConsole)
        {
            return this;
        }
        if (build is null)
        {
            _ = Run();
        }

        var output = GetCompilationOutput(true);
        Console.WriteLine(output);
        wroteToConsole = true;
        return this;
    }

    public SourceGeneratorTest OutputSteps(params string[] specificStages)
    {
        RunClonedBuild();

        var stagesToTrack = specificStages.Length != 0 ? specificStages : generatorStagesList.ToArray();
        var wrapperType = typeof(ISourceGenerator).Assembly.GetType("Microsoft.CodeAnalysis.IncrementalGeneratorWrapper", throwOnError: true);
        var generatorPropertyGetter = wrapperType!.GetProperty("Generator", BindingFlags.Instance | BindingFlags.NonPublic)!.GetMethod!;

        foreach (var result in clonedBuild.RunResult.Results)
        {
            var generatorType = result.Generator.GetType();
            if (generatorType == wrapperType)
            {
                var innerGenerator = generatorPropertyGetter.Invoke(result.Generator, []);
                if (innerGenerator is not null)
                {
                    generatorType = innerGenerator.GetType();
                }
            }
            Console.WriteLine($"## {generatorType.Name} Results");
            Console.WriteLine();

            foreach (var stepName in stagesToTrack)
            {
                var namedStep = result.TrackedSteps[stepName];
                var outputs = namedStep.SelectMany(runStep => runStep.Outputs).ToArray();
                var outputCount = outputs.Length;
                var reasons = outputs.Select(o => o.Reason).GroupBy(reason => reason)
                    .Select(g => $"{g.Count()} {g.Key}")
                    .ToArray();

                Console.WriteLine($"Step {stepName} -  {outputCount} total outputs, {string.Join(", ", reasons)}");

                foreach (var output in outputs)
                {
                    Console.WriteLine($"- [{output.Reason}] {output.Value}");
                }

                Console.WriteLine();
            }
        }

        return this;
    }

    IEnumerable<SyntaxTree> FilteredSyntaxTrees()
    {
        if (build is null)
        {
            throw new Exception("This shouldn't have happened yet.");
        }

        return outputType switch
        {
            GeneratorTestOutput.All => build.OutputCompilation.SyntaxTrees,
            GeneratorTestOutput.GeneratedOnly => build.OutputCompilation.SyntaxTrees.Where(t => t.FilePath.EndsWith(".g.cs")),
            GeneratorTestOutput.SourceOnly => build.OutputCompilation.SyntaxTrees.Where(t => !t.FilePath.EndsWith(".g.cs")),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    class Build
    {
        readonly Compilation initialCompilation;
        readonly GeneratorDriver driver;

        public Build(Compilation initialCompilation, GeneratorDriver driver)
        {
            this.initialCompilation = initialCompilation;
            this.driver = driver.RunGeneratorsAndUpdateCompilation(initialCompilation, out var outputCompilation, out var generatorDiagnostics);

            RunResult = this.driver.GetRunResult();
            OutputCompilation = outputCompilation;
            GeneratorDiagnostics = generatorDiagnostics;
        }

        public Compilation OutputCompilation { get; }
        public ImmutableArray<Diagnostic> GeneratorDiagnostics { get; }
        public GeneratorDriverRunResult RunResult { get; }

        public Build Clone()
        {
            var cloneCompilation = initialCompilation.Clone();
            return new Build(cloneCompilation, driver);
        }
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

public enum GeneratorTestOutput
{
    GeneratedOnly = 0,
    SourceOnly = 1,
    All = 2
}