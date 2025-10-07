namespace NServiceBus.Core.Analyzer.Tests.AssemblyScanning;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

[TestFixture]
public class AssemblyScanningTests
{
    [Test]
    public void Basic()
    {
        var source = $$"""
                       using System;
                       using System.Threading;
                       using System.Threading.Tasks;
                       using NServiceBus;
                       using NServiceBus.Features;
                       using NServiceBus.Extensibility;
                       using NServiceBus.Installation;
                       using NServiceBus.Sagas;
                       
                       namespace UserCode;
                       
                       public class Program
                       {
                           public void Main()
                           {
                               var cfg = new EndpointConfiguration("UserCode");
                               cfg.TurnOffAssemblyScanningAndUseSourceGenerationInstead(true);
                               cfg.TurnOffAssemblyScanningAndUseSourceGenerationInstead(false);
                           }
                       }
                       
                       public class MyEvent : IEvent {}
                       public class MyCommand : ICommand {}
                       public class MyMessage : IMessage {}
                       public class MyFeature : Feature
                       {
                           protected override void Setup(FeatureConfigurationContext context) { }
                       }
                       public class MyHandler : IHandleMessages<MyEvent>
                       {
                           public Task Handle(MyEvent message, IMessageHandlerContext context) => Task.CompletedTask;
                       }
                       public class MySecondHandler : IHandleMessages<MyCommand>
                       {
                           public Task Handle(MyCommand message, IMessageHandlerContext context) => Task.CompletedTask;
                       }
                       public class Installer : INeedToInstallSomething
                       {
                           public Task Install(string identity, CancellationToken cancellationToken) => Task.CompletedTask;
                       }
                       public class NotInteresting { }

                       public class DownstreamSpecificType : IAmImportantNonCoreType { }

                       [NServiceBusExtensionPoint("RegisterImportant", true)]
                       public interface IAmImportantNonCoreType { }
                       
                       public class MySaga : Saga<MySagaData>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper) { }
                       }
                       
                       public class MySagaData : ContainSagaData { }
                       
                       public class MySagaNotFound : IHandleSagaNotFound
                       {
                           public Task Handle(object message, IMessageProcessingContext context) => Task.CompletedTask;
                       }
                       """;

        var (output, _) = GetGeneratedOutput(source);

        // Assert.Multiple(() =>
        // {
        //     // Assert the generated registry contains expected types
        //     Assert.That(output, Does.Contain("typeof(UserCode.MyEvent)"));
        //     Assert.That(output, Does.Contain("typeof(UserCode.MyCommand)"));
        //     Assert.That(output, Does.Contain("typeof(UserCode.MyMessage)"));
        //     Assert.That(output, Does.Contain("typeof(UserCode.MyHandler)"));
        //     Assert.That(output, Does.Contain("typeof(UserCode.MySecondHandler)"));
        //     Assert.That(output, Does.Contain("typeof(UserCode.Installer)"));
        //     Assert.That(output, Does.Contain("typeof(UserCode.DownstreamSpecificType)"));
        //     Assert.That(output, Does.Contain("typeof(UserCode.IAmImportantNonCoreType)"));
        //     Assert.That(output, Does.Contain("typeof(UserCode.MyFeature)"));
        //
        //     // Assert the generated registry does not contain expected types
        //     Assert.That(output, Does.Not.Contain("typeof(UserCode.NotInteresting)"));
        // });

        Console.Write(output);
    }

    static (string output, ImmutableArray<Diagnostic> diagnostics) GetGeneratedOutput(string source, bool suppressGeneratedDiagnosticsErrors = false)
    {
        var features = new Dictionary<string, string>
        {
            ["InterceptorsNamespaces"] = "NServiceBus",
            ["InterceptorsPreviewNamespaces"] = "NServiceBus",
        };

        var parseOptions = new CSharpParseOptions(LanguageVersion.LatestMajor)
            .WithFeatures(features);

        string[] sources = [source];

        var syntaxTrees = sources
            .Select(x =>
            {
                var tree = CSharpSyntaxTree.ParseText(x, path: "Source.cs");
                var options = parseOptions;
                return tree.WithRootAndOptions(tree.GetRoot(), options);
            });

        var references = new List<MetadataReference>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        // Add necessary references for the generated trigger
        references.Add(MetadataReference.CreateFromFile(typeof(IMessage).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(EndpointConfiguration).Assembly.Location));

        IIncrementalGenerator[] generators = [new KnownTypesGenerator(), new AssemblyScanningGenerator()];

        var opts = new GeneratorDriverOptions(disabledOutputs: IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true);

        GeneratorDriver driver =
            CSharpGeneratorDriver.Create(
                generators.Select(x => x.AsSourceGenerator()),
                driverOptions: opts,
                optionsProvider: new OptionsProvider(new DictionaryAnalyzerOptions(features)),
                parseOptions: parseOptions);


        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        var initialCompilation = CSharpCompilation.Create("initial", syntaxTrees, references, options);

        driver.RunGeneratorsAndUpdateCompilation(initialCompilation, out var outputCompilation, out var generatedDiagnostics);

        if (!suppressGeneratedDiagnosticsErrors)
        {
            Assert.That(generatedDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), Is.False, "Failed: " + generatedDiagnostics.FirstOrDefault()?.GetMessage());
        }

        // Verify the code compiled:
        var compilationErrors = outputCompilation
            .GetDiagnostics()
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .ToArray();

        Assert.That(compilationErrors, Is.Empty, compilationErrors.FirstOrDefault()?.GetMessage());

        var generated = outputCompilation.SyntaxTrees
            .Where(st => st.FilePath != "Source.cs")
            .Select(st => $"""
                          // ========================================================================================
                          // {st.FilePath}
                          // ========================================================================================
                          {st}
                          """);
        string combinedGeneration = string.Join("", generated);

        return (combinedGeneration, generatedDiagnostics);
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